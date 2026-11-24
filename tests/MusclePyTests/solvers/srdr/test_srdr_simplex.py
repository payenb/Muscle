# Muscle

# Copyright <2015-2025> <Université catholique de Louvain (UCLouvain)>

# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at

#     http://www.apache.org/licenses/LICENSE-2.0

# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# List of the contributors to the development of Muscle: see NOTICE file.
# Description and complete License: see NOTICE file.
# This fork (Muscle 2.0): modified by Basile Payen (UCLouvain) with Claude (Anthropic).

import unittest
import numpy as np
from musclepy.femodel.pytruss import PyTruss
from musclepy.femodel.pynodes import PyNodes
from musclepy.femodel.pyelements import PyElements
from musclepy.solvers.dr.main import main_dynamic_relaxation
from musclepy.solvers.dr.py_config_dr import PyConfigDR
from musclepy.solvers.srdr.main import main_stiffness_reduction_dr
from musclepy.solvers.srdr.py_config_srdr import PyConfigSRDR


class TestSRDR_Simplex(unittest.TestCase):
    """Reuses the exact simplex geometry/loading of MusclePyTests.solvers.dr.test_dr_simplex,
    which is itself the aluminum simplex case study of Payen et al. 2026 (same strut cross
    section: hollow circular, outside diameter 60mm, thickness 2mm, E=70390 MPa)."""

    def setUp(self):
        self.nodes = PyNodes(
            initial_coordinates=np.array([
                [0.00, -2043.82, 0.00],
                [0.00, 0.00, 0.00],
                [1770.00, -1021.91, 0.00],
                [590.00, -2201.91, 1950.00],
                [-431.91, -431.91, 1950.00],
                [1611.91, -431.91, 1950.00]
            ]) * 1e-3,
            dof=np.array([
                [False, True, False],
                [False, False, False],
                [True, True, False],
                [True, True, True],
                [True, True, True],
                [True, True, True]
            ])
        )

        # area [m^2], youngs [Pa] and inertia [m^4]: SI, like every musclepy input, and
        # exactly what MuscleApp sends unconverted from Element.CS.Area, Element.Material.Ec/Et
        # and Element.CS.Inertia.
        area = np.ones(12)
        area[0:3] = 364.4e-6  # Struts area, m^2 (364.4 mm^2)
        area[3:12] = 50.3e-6  # Cables area, m^2 (50.3 mm^2)

        # Hollow circular strut section: outside diameter 60mm, thickness 2mm (Table 1 of [1]).
        Ro, Ri = 30e-3, 28e-3  # [m]
        strut_inertia = np.pi / 4 * (Ro ** 4 - Ri ** 4)  # [m^4]
        self.inertia = np.zeros(12)
        self.inertia[0:3] = strut_inertia

        self.elements = PyElements(
            nodes=self.nodes,
            type=np.array([-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1]),
            end_nodes=np.array([
                [2, 4], [1, 3], [0, 5],
                [1, 2], [0, 1], [0, 2], [4, 5], [3, 4], [3, 5], [2, 5], [1, 4], [0, 3]
            ]),
            area=area,
            youngs=np.array([
                [70390e6, 0], [70390e6, 0], [70390e6, 0],
                [0, 71750e6], [0, 71750e6], [0, 71750e6], [0, 71750e6], [0, 71750e6],
                [0, 71750e6], [0, 71750e6], [0, 71750e6], [0, 71750e6]
            ])
        )

        self.structure = PyTruss(self.nodes, self.elements)

        self.loads = np.zeros((6, 3))
        self.loads[0:3, 2] = 45.7
        self.loads[3:6, 2] = 41.6

        self.delta_free_length = np.zeros(12)
        self.delta_free_length[0:3] = 0.835e-3
        self.delta_free_length[8] = -35e-3

    def test_zero_imperfection_matches_plain_dr(self):
        """With imperfection = 0 for every element, Kred/K = 1 always, so the SRDR outer
        loop must converge in a single (extra) iteration and reproduce exactly the plain
        Dynamic Relaxation result. This is the strongest regression test available: it
        does not depend on any new reference data, only on internal consistency."""
        plain_result = main_dynamic_relaxation(
            structure=self.structure,
            loads_increment=self.loads,
            free_length_variation=self.delta_free_length,
            config=PyConfigDR(zero_residual_rtol=1e-4, zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100)
        )

        srdr_config = PyConfigSRDR()
        srdr_result = main_stiffness_reduction_dr(
            structure=self.structure,
            loads_increment=self.loads,
            free_length_variation=self.delta_free_length,
            dr_config=PyConfigDR(zero_residual_rtol=1e-4, zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100),
            srdr_config=srdr_config,
            inertia=self.inertia,
            imperfection=np.zeros(12)
        )

        self.assertTrue(srdr_config.converged)
        self.assertLessEqual(srdr_config.n_outer_iterations, 2)
        np.testing.assert_allclose(srdr_result.elements.tension, plain_result.elements.tension, rtol=1e-3)
        np.testing.assert_allclose(srdr_result.nodes.coordinates, plain_result.nodes.coordinates, rtol=1e-3, atol=1e-6)

    def test_default_imperfection_converges_and_reduces_strut_forces(self):
        """With the default imperfection ratio (e0/L0 = 1/300, Eurocode 9), the SRDR loop
        must converge, and the resulting strut compression forces should differ from the
        plain (perfect-strut) DR result -- this is the whole point of the method."""
        plain_result = main_dynamic_relaxation(
            structure=self.structure,
            loads_increment=self.loads,
            free_length_variation=self.delta_free_length,
            config=PyConfigDR(zero_residual_rtol=1e-4, zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100)
        )

        srdr_config = PyConfigSRDR(force_tol=1.0, max_outer_iterations=100)
        srdr_result = main_stiffness_reduction_dr(
            structure=self.structure,
            loads_increment=self.loads,
            free_length_variation=self.delta_free_length,
            dr_config=PyConfigDR(zero_residual_rtol=1e-4, zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100),
            srdr_config=srdr_config,
            inertia=self.inertia
            # imperfection not provided -> defaults to free_length/300 for every element
        )

        self.assertTrue(srdr_config.converged, f"SRDR did not converge within {srdr_config.max_outer_iterations} iterations")
        self.assertFalse(np.any(np.isnan(srdr_result.elements.tension)))

        strut_tension_plain = plain_result.elements.tension[0:3]
        strut_tension_srdr = srdr_result.elements.tension[0:3]
        self.assertFalse(np.allclose(strut_tension_plain, strut_tension_srdr, rtol=1e-3),
                          "Imperfect-strut tensions should differ from the perfect-strut result")

        # Struts in compression should show a nonzero second-order moment/deflection; cables
        # (indices 3-11) must stay at exactly 0 since the beam-column model doesn't apply to them.
        self.assertFalse(np.any(np.isnan(srdr_result.elements.moment)))
        self.assertFalse(np.any(np.isnan(srdr_result.elements.deflection)))
        self.assertTrue(np.all(srdr_result.elements.deflection[0:3] > 0))
        self.assertTrue(np.all(srdr_result.elements.moment[0:3] > 0))
        np.testing.assert_array_equal(srdr_result.elements.moment[3:12], 0.0)
        np.testing.assert_array_equal(srdr_result.elements.deflection[3:12], 0.0)


if __name__ == '__main__':
    unittest.main()
