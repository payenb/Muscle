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
from musclepy.femodel.pynodes import PyNodes
from musclepy.femodel.pyelements import PyElements
from musclepy.femodel.pytruss import PyTruss
from musclepy.solvers.stability.main import main_prestress_stability
from musclepy.solvers.dr.main import main_dynamic_relaxation
from musclepy.solvers.dr.py_config_dr import PyConfigDR


def _aligned(tension):
    """Two aligned elements 0-1-2 along x, middle node free in x and z: one mechanism, along z."""
    nodes = PyNodes(initial_coordinates=np.array([[0.0, 0, 0], [1.0, 0, 0], [2.0, 0, 0]]),
                    dof=np.array([[False] * 3, [True, False, True], [False] * 3]))
    elements = PyElements(nodes=nodes, type=np.array([1, 1]), end_nodes=np.array([[0, 1], [1, 2]]),
                          area=np.full(2, 50e-6), youngs=np.full((2, 2), 70e9), tension=np.full(2, tension))
    return PyTruss(nodes, elements)


def _simplex():
    """Simplex of Feron et al. (2023), prestressed by dynamic relaxation (1 mechanism, 1 self-stress)."""
    coordinates = np.array([[0.00, -2043.82, 0.00], [0.00, 0.00, 0.00], [1770.00, -1021.91, 0.00],
                            [590.00, -2201.91, 1950.00], [-431.91, -431.91, 1950.00],
                            [1611.91, -431.91, 1950.00]]) * 1e-3
    dof = np.array([[False, True, False], [False, False, False], [True, True, False],
                    [True, True, True], [True, True, True], [True, True, True]])
    end_nodes = np.array([[2, 4], [1, 3], [0, 5], [1, 2], [0, 1], [0, 2],
                          [4, 5], [3, 4], [3, 5], [2, 5], [1, 4], [0, 3]])
    youngs = np.zeros((12, 2))
    youngs[0:3, 0] = 70390e6
    youngs[3:12, 1] = 71750e6
    area = np.r_[np.full(3, 364.4e-6), np.full(9, 50.3e-6)]
    nodes = PyNodes(initial_coordinates=coordinates, dof=dof)
    elements = PyElements(nodes=nodes, type=np.r_[-np.ones(3), np.ones(9)].astype(int),
                          end_nodes=end_nodes, area=area, youngs=youngs)
    free_length_variation = np.zeros(12)
    free_length_variation[0:3] = 0.835e-3  # lengthen the struts
    return main_dynamic_relaxation(PyTruss(nodes, elements), np.zeros((6, 3)), free_length_variation,
                                   PyConfigDR(zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100))


class TestPrestressStability(unittest.TestCase):

    def test_tensioned_aligned_cables_are_prestress_stable(self):
        t = 5000.0
        result = main_prestress_stability(_aligned(t))
        self.assertEqual(result.mechanism_count, 1)
        self.assertEqual(result.status, "stable")
        self.assertAlmostEqual(result.min_eigenvalue, 2 * t / 1.0, places=6)  # 2 t/L along z
        np.testing.assert_allclose(result.critical_mode.reshape(3, 3)[1], [0, 0, 1], atol=1e-12)

    def test_unstressed_aligned_cables_are_critical(self):
        result = main_prestress_stability(_aligned(0.0))
        self.assertEqual(result.status, "critical")

    def test_compressed_aligned_bars_are_unstable(self):
        result = main_prestress_stability(_aligned(-5000.0))
        self.assertEqual(result.status, "unstable")
        self.assertEqual(result.negative_count, 1)

    def test_tension_argument_overrides_the_structure_state(self):
        structure = _aligned(0.0)
        self.assertEqual(main_prestress_stability(structure, tension=np.full(2, 1.0)).status, "stable")
        self.assertEqual(main_prestress_stability(structure, tension=np.full(2, -1.0)).status, "unstable")

    def test_kinematically_determinate_truss_is_stable(self):
        """A triangle on 3 pinned supports' worth of constraints has no mechanism."""
        nodes = PyNodes(initial_coordinates=np.array([[0.0, 0, 0], [2.0, 0, 0], [1.0, 0, 1.0]]),
                        dof=np.array([[False] * 3, [False] * 3, [True, False, True]]))
        elements = PyElements(nodes=nodes, type=np.array([-1, -1]), end_nodes=np.array([[0, 2], [1, 2]]),
                              area=np.full(2, 1e-4), youngs=np.full((2, 2), 210e9), tension=np.array([-1.0, -1.0]))
        result = main_prestress_stability(PyTruss(nodes, elements))
        self.assertEqual(result.mechanism_count, 0)
        self.assertEqual(result.status, "stable")
        self.assertEqual(result.critical_mode.size, 0)

    def test_simplex_is_prestress_stable_and_reversed_prestress_is_not(self):
        simplex = _simplex()
        self.assertTrue(np.all(simplex.elements.tension[3:] > 0))  # cables tight, struts compressed

        result = main_prestress_stability(simplex)
        self.assertEqual(result.mechanism_count, 1)
        self.assertEqual(result.status, "stable")

        # Any positive multiple of the prestress gives the same status; the opposite state (cables
        # compressed, struts tensioned) destabilises the mechanism.
        self.assertEqual(main_prestress_stability(simplex, tension=1e-3 * simplex.elements.tension).status,
                         "stable")
        self.assertEqual(main_prestress_stability(simplex, tension=-simplex.elements.tension).status,
                         "unstable")


if __name__ == '__main__':
    unittest.main()
