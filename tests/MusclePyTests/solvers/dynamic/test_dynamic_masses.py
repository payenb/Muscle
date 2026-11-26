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
from musclepy.solvers.dynamic.main import main_dynamic_modal_analysis


def _two_bars():
    """Two bars from a pinned support (node 0) and a roller free in x (node 2) to an apex
    (node 1) free in x and z."""
    nodes = PyNodes(initial_coordinates=np.array([[0.0, 0, 0], [1.0, 0, 1.0], [2.0, 0, 0]]),
                    dof=np.array([[False, False, False], [True, False, True], [True, False, False]]))
    elements = PyElements(nodes=nodes, type=np.array([-1, -1]), end_nodes=np.array([[0, 1], [1, 2]]),
                          area=np.full(2, 364.4e-6), youngs=np.full((2, 2), 70e9))
    return PyTruss(nodes, elements)


class TestDynamicMasses(unittest.TestCase):

    def test_free_dof_without_mass_is_named(self):
        """A free DOF without mass makes the mass matrix singular: the error must name it
        (here the roller, node 2 in x) instead of a cryptic eigensolver message."""
        masses = np.zeros((3, 3))
        masses[1] = 1.0  # apex only
        with self.assertRaises(ValueError) as caught:
            main_dynamic_modal_analysis(_two_bars(), point_masses=masses)
        self.assertIn("node 2 (x)", str(caught.exception))
        self.assertNotIn("node 1", str(caught.exception))

    def test_nodal_masses_on_every_free_node_are_enough(self):
        masses = np.zeros((3, 3))
        masses[1] = 1.0
        masses[2] = 0.5
        results = main_dynamic_modal_analysis(_two_bars(), point_masses=masses)
        self.assertEqual(results.mode_count, 3)
        self.assertTrue(np.all(results.frequencies > 0))

    def test_lumped_element_masses_equal_half_masses_at_the_nodes(self):
        """Option 1 (lumped) is the same as adding half of each element mass to its end nodes."""
        element_masses = np.array([2.0, 3.0])
        lumped = main_dynamic_modal_analysis(_two_bars(), element_masses=element_masses, element_masses_option=1)
        nodal = np.zeros((3, 3))
        nodal[0] = 1.0
        nodal[1] = 1.0 + 1.5
        nodal[2] = 1.5
        by_hand = main_dynamic_modal_analysis(_two_bars(), point_masses=nodal)
        np.testing.assert_allclose(lumped.frequencies, by_hand.frequencies, rtol=1e-12)


if __name__ == '__main__':
    unittest.main()
