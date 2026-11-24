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
from musclepy.femodel.prestress import free_length_from_tension, structure_with_tension
from musclepy.solvers.svd.main import main_singular_value_decomposition
from musclepy.solvers.dr.main import main_dynamic_relaxation
from musclepy.solvers.dr.py_config_dr import PyConfigDR


def _simplex():
    """Simplex of Feron et al. (2023), nominal geometry, no prestress."""
    coordinates = np.array([[0.00, -2043.82, 0.00], [0.00, 0.00, 0.00], [1770.00, -1021.91, 0.00],
                            [590.00, -2201.91, 1950.00], [-431.91, -431.91, 1950.00],
                            [1611.91, -431.91, 1950.00]]) * 1e-3
    dof = np.array([[False, True, False], [False, False, False], [True, True, False],
                    [True, True, True], [True, True, True], [True, True, True]])
    end_nodes = np.array([[2, 4], [1, 3], [0, 5], [1, 2], [0, 1], [0, 2],
                          [4, 5], [3, 4], [3, 5], [2, 5], [1, 4], [0, 3]])
    youngs = np.zeros((12, 2))
    youngs[0:3, 0] = 70390e6  # struts: compression only
    youngs[3:12, 1] = 71750e6  # cables: tension only
    area = np.r_[np.full(3, 364.4e-6), np.full(9, 50.3e-6)]
    nodes = PyNodes(initial_coordinates=coordinates, dof=dof)
    elements = PyElements(nodes=nodes, type=np.r_[-np.ones(3), np.ones(9)].astype(int),
                          end_nodes=end_nodes, area=area, youngs=youngs)
    return PyTruss(nodes, elements)


def _self_stress(structure, strut_force=-1500.0):
    """[N] the self-stress state of the simplex, scaled so that the struts carry `strut_force`."""
    vs = main_singular_value_decomposition(structure).Vs[:, 0]
    return vs / vs[0] * strut_force


class TestFreeLengthFromTension(unittest.TestCase):

    def test_formula_gives_back_the_tension(self):
        """t = EA/L0 * (L - L0) with the free lengths computed, and the right modulus for each sign."""
        structure = _simplex()
        tension = _self_stress(structure)
        L0 = free_length_from_tension(structure.elements, tension)
        L = structure.elements.current_length
        E = np.where(tension > 0, structure.elements.youngs[:, 1], structure.elements.youngs[:, 0])
        np.testing.assert_allclose(E * structure.elements.area / L0 * (L - L0), tension, rtol=1e-12)
        self.assertTrue(np.all(L0[:3] > L[:3]))  # compressed struts are longer than their geometry
        self.assertTrue(np.all(L0[3:] < L[3:]))  # tensioned cables are shorter

    def test_zero_tension_keeps_the_current_length(self):
        structure = _simplex()
        np.testing.assert_array_equal(free_length_from_tension(structure.elements, np.zeros(12)),
                                      structure.elements.current_length)

    def test_cable_in_compression_is_refused(self):
        structure = _simplex()
        tension = np.zeros(12)
        tension[5] = -10.0  # a cable has no compression stiffness
        with self.assertRaises(ValueError):
            free_length_from_tension(structure.elements, tension)

    def test_compression_beyond_EA_is_refused(self):
        structure = _simplex()
        tension = np.zeros(12)
        tension[0] = -1.01 * 70390e6 * 364.4e-6
        with self.assertRaises(ValueError):
            free_length_from_tension(structure.elements, tension)


class TestStructureWithTension(unittest.TestCase):

    def test_dynamic_relaxation_keeps_the_prestress(self):
        """A self-stress state applied with matching free lengths is an equilibrium: the DR keeps it.
        Without the free lengths (tension only), the DR recomputes the forces from the free lengths
        and the prestress is lost."""
        structure = _simplex()
        tension = _self_stress(structure)
        config = PyConfigDR(zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100)

        prestressed = structure_with_tension(structure, tension)
        self.assertIsInstance(prestressed, PyTruss)
        np.testing.assert_array_equal(prestressed.elements.tension, tension)
        result = main_dynamic_relaxation(prestressed, np.zeros((6, 3)), np.zeros(12), config)
        np.testing.assert_allclose(result.elements.tension, tension, rtol=1e-6)
        self.assertLess(np.max(np.abs(result.nodes.displacements)), 1e-9)

        tension_only = PyTruss(structure.nodes, structure.elements.copy_and_update(structure.nodes, tension=tension))
        lost = main_dynamic_relaxation(tension_only, np.zeros((6, 3)), np.zeros(12),
                                       PyConfigDR(zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100))
        self.assertLess(np.max(np.abs(lost.elements.tension)), 1e-3)

    def test_input_structure_is_not_modified(self):
        structure = _simplex()
        free_length = structure.elements.free_length.copy()
        structure_with_tension(structure, _self_stress(structure))
        np.testing.assert_array_equal(structure.elements.free_length, free_length)
        np.testing.assert_array_equal(structure.elements.tension, np.zeros(12))


if __name__ == '__main__':
    unittest.main()
