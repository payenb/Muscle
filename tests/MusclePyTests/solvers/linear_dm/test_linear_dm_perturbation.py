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
from musclepy.solvers.dm.linear_dm import main_linear_displacement_method, perturb
from musclepy.solvers.dm.nonlinear_dm import main_nonlinear_displacement_method


class TestLinearDM_Perturbation(unittest.TestCase):
    """A singular stiffness matrix makes the displacement methods perturb the geometry at random.
    The perturbation is seeded, so the same input must always give the same output. Results are
    compared with a tight tolerance rather than bit for bit, because multithreaded BLAS varies by
    ~1e-13 between runs; an unseeded perturbation changes them by orders of magnitude more."""

    def setUp(self):
        # Two aligned, unstressed cables: the stiffness matrix has no stiffness along z at the
        # middle node, hence is singular.
        nodes = PyNodes(
            initial_coordinates=np.array([[0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [2.0, 0.0, 0.0]]),
            dof=np.array([[False, False, False], [True, False, True], [False, False, False]]),
        )
        elements = PyElements(
            nodes=nodes,
            type=np.array([1, 1]),
            end_nodes=np.array([[0, 1], [1, 2]]),
            area=np.full(2, 2500e-6),  # [m²]
            youngs=np.full((2, 2), 10e9),  # [Pa]
        )
        self.structure = PyTruss(nodes, elements)
        self.loads = np.zeros(9)
        self.loads[5] = -100000.0  # [N] vertical load on the middle node

    def test_perturb_is_reproducible(self):
        a = perturb(self.structure)
        b = perturb(self.structure)
        np.testing.assert_array_equal(a.nodes.coordinates, b.nodes.coordinates)
        self.assertFalse(np.array_equal(a.nodes.coordinates, self.structure.nodes.coordinates))
        # supports are never moved
        fixed = ~self.structure.nodes.dof
        np.testing.assert_array_equal(a.nodes.coordinates[fixed], self.structure.nodes.coordinates[fixed])

    def test_linear_dm_on_singular_structure_is_reproducible(self):
        first = main_linear_displacement_method(self.structure, self.loads, np.zeros(2))
        second = main_linear_displacement_method(self.structure, self.loads, np.zeros(2))
        np.testing.assert_allclose(first.nodes.displacements, second.nodes.displacements, rtol=1e-9, atol=1e-12)
        np.testing.assert_allclose(first.elements.tension, second.elements.tension, rtol=1e-9, atol=1e-6)

    def test_nonlinear_dm_on_singular_structure_is_reproducible(self):
        first = main_nonlinear_displacement_method(self.structure, self.loads, n_steps=100)
        second = main_nonlinear_displacement_method(self.structure, self.loads, n_steps=100)
        np.testing.assert_allclose(first.nodes.displacements, second.nodes.displacements, rtol=1e-9, atol=1e-12)
        np.testing.assert_allclose(first.elements.tension, second.elements.tension, rtol=1e-9, atol=1e-6)


if __name__ == '__main__':
    unittest.main()
