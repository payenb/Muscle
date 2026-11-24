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
import warnings
import numpy as np
from musclepy.femodel.pynodes import PyNodes
from musclepy.femodel.pyelements import PyElements
from musclepy.femodel.pytruss import PyTruss
from musclepy.utils.matrix_calculations import compute_tangent_stiffness_matrix
from musclepy.solvers.stability.main import main_tangent_stiffness_stability
from musclepy.solvers.dr.main import main_dynamic_relaxation
from musclepy.solvers.dr.py_config_dr import PyConfigDR
from musclepy.solvers.dynamic.main import main_dynamic_modal_analysis

E = 70e9  # [Pa]
A = 50e-6  # [m²]


def _structure(coordinates, dof, end_nodes, tension, types=None, youngs=None):
    """Truss whose free lengths match the given tensions: t = EA/L0 (L - L0)."""
    coordinates = np.asarray(coordinates, dtype=float)
    end_nodes = np.asarray(end_nodes)
    tension = np.asarray(tension, dtype=float)
    b = len(end_nodes)
    L = np.linalg.norm(coordinates[end_nodes[:, 1]] - coordinates[end_nodes[:, 0]], axis=1)
    free_length = L / (1 + tension / (E * A))
    nodes = PyNodes(initial_coordinates=coordinates, dof=np.asarray(dof))
    elements = PyElements(nodes=nodes,
                          type=np.ones(b, dtype=int) if types is None else np.asarray(types),
                          end_nodes=end_nodes, area=np.full(b, A),
                          youngs=np.full((b, 2), E) if youngs is None else np.asarray(youngs, dtype=float),
                          free_length=free_length, tension=tension)
    return PyTruss(nodes, elements)


def _internal_forces(structure, coordinates):
    """[N] nodal forces exerted by the elements, sum of t*e at the end node and -t*e at the start
    node, with t = E*A/L0 * (L - L0) recomputed from the coordinates (E: tension or compression
    modulus depending on the sign of L - L0)."""
    el = structure.elements
    x0 = coordinates[el.end_nodes[:, 0]]
    x1 = coordinates[el.end_nodes[:, 1]]
    L = np.linalg.norm(x1 - x0, axis=1)
    e = (x1 - x0) / L[:, None]
    young = np.where(L - el.free_length > 0, el.youngs[:, 1], el.youngs[:, 0])
    t = young * el.area / el.free_length * (L - el.free_length)
    f = np.zeros_like(coordinates)
    np.add.at(f, el.end_nodes[:, 1], t[:, None] * e)
    np.add.at(f, el.end_nodes[:, 0], -t[:, None] * e)
    return f.reshape(-1)


class TestTangentStiffness(unittest.TestCase):

    def test_equals_jacobian_of_internal_forces(self):
        """K_t is the derivative of the internal forces: compare with central finite differences
        on a skew 3D truss, prestressed (both cables in tension and bars in compression)."""
        coordinates = np.array([[0.0, 0.0, 0.0], [1.3, 0.2, 0.1], [0.4, 1.1, -0.3], [0.5, 0.4, 1.2]])
        end_nodes = np.array([[0, 1], [0, 2], [0, 3], [1, 2], [1, 3], [2, 3]])
        tension = np.array([1500.0, -800.0, 2200.0, 400.0, -1200.0, 900.0])
        types = np.where(tension > 0, 1, -1)
        youngs = np.where(tension[:, None] > 0, [[0.0, E]], [[E, 0.0]])  # cables in tension, bars in compression
        structure = _structure(coordinates, np.ones((4, 3), dtype=bool), end_nodes, tension, types=types,
                               youngs=youngs)

        K = compute_tangent_stiffness_matrix(structure)
        h = 1e-7
        K_fd = np.zeros_like(K)
        for j in range(12):
            dx = np.zeros(12)
            dx[j] = h
            plus = _internal_forces(structure, coordinates + dx.reshape(4, 3))
            minus = _internal_forces(structure, coordinates - dx.reshape(4, 3))
            K_fd[:, j] = (plus - minus) / (2 * h)
        np.testing.assert_allclose(K, K_fd, rtol=1e-6, atol=1e-6 * np.max(np.abs(K)))

    def test_axial_stiffness_is_EA_over_L0(self):
        """Two aligned cables in tension t: along the cables the stiffness is 2 EA/L0 exactly
        (the former formulation added t/L), across them it is 2 t/L."""
        t = 5000.0
        structure = _structure([[0, 0, 0], [1, 0, 0], [2, 0, 0]],
                               [[False] * 3, [True, False, True], [False] * 3], [[0, 1], [1, 2]], [t, t])
        K = compute_tangent_stiffness_matrix(structure)
        L0 = structure.elements.free_length[0]
        L = structure.elements.current_length[0]
        self.assertAlmostEqual(K[3, 3] / (2 * E * A / L0), 1.0, places=12)
        self.assertAlmostEqual(K[5, 5] / (2 * t / L), 1.0, places=12)
        self.assertAlmostEqual(K[3, 5], 0.0, places=6)


class TestTangentStiffnessStability(unittest.TestCase):

    def _aligned(self, t, types=(1, 1)):
        return _structure([[0, 0, 0], [1, 0, 0], [2, 0, 0]],
                          [[False] * 3, [True, False, True], [False] * 3], [[0, 1], [1, 2]], [t, t],
                          types=types)

    def test_prestressed_aligned_cables_are_stable(self):
        t = 5000.0
        result = main_tangent_stiffness_stability(self._aligned(t))
        self.assertEqual(result.status, "stable")
        self.assertTrue(result.is_stable)
        self.assertEqual(result.negative_count, 0)
        L = 1.0 + 0.0  # current length
        self.assertAlmostEqual(result.min_eigenvalue, 2 * t / L, places=6)  # transverse mode
        np.testing.assert_allclose(result.critical_mode.reshape(3, 3)[1], [0, 0, 1], atol=1e-12)

    def test_unstressed_aligned_cables_are_critical(self):
        result = main_tangent_stiffness_stability(self._aligned(0.0))
        self.assertEqual(result.status, "critical")
        self.assertEqual(result.negative_count, 0)

    def test_compressed_aligned_bars_are_unstable(self):
        t = -5000.0
        result = main_tangent_stiffness_stability(self._aligned(t, types=(-1, -1)))
        self.assertEqual(result.status, "unstable")
        self.assertEqual(result.negative_count, 1)
        L = 1.0
        self.assertAlmostEqual(result.min_eigenvalue, 2 * t / L, places=6)
        np.testing.assert_allclose(result.critical_mode.reshape(3, 3)[1], [0, 0, 1], atol=1e-12)

    def test_form_found_simplex_is_stable(self):
        """Simplex tensegrity of Feron et al. (2023), prestressed by dynamic relaxation."""
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
        result = main_dynamic_relaxation(PyTruss(nodes, elements), np.zeros((6, 3)), free_length_variation,
                                         PyConfigDR(zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100))
        self.assertTrue(np.all(result.elements.tension[3:] > 0))  # all cables tight

        stability = main_tangent_stiffness_stability(result)
        self.assertEqual(stability.status, "stable")
        self.assertGreater(stability.min_eigenvalue_normalized, 1e-6)


class TestModalAnalysisOfUnstableStructure(unittest.TestCase):

    def test_negative_eigenvalue_gives_negative_frequency(self):
        """Two aligned bars in compression, unit mass on the middle node: the transverse mode has
        omega² = 2t/(L m) < 0. It must come out as a negative frequency, not as sqrt(|omega²|)."""
        t = -5000.0
        structure = _structure([[0, 0, 0], [1, 0, 0], [2, 0, 0]],
                               [[False] * 3, [True, False, True], [False] * 3], [[0, 1], [1, 2]], [t, t],
                               types=(-1, -1))
        masses = np.zeros((3, 3))
        masses[1] = 1.0  # [kg]
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            results = main_dynamic_modal_analysis(structure, point_masses=masses)
        self.assertTrue(any(issubclass(w.category, RuntimeWarning) for w in caught))

        expected = -np.sqrt(-2 * t / 1.0 / 1.0) / (2 * np.pi)
        self.assertAlmostEqual(results.frequencies[0], expected, places=9)
        self.assertGreater(results.frequencies[1], 0)
        self.assertEqual(results.unstable_mode_count, 1)
        self.assertAlmostEqual(results.eigenvalues[0], 2 * t, places=6)

    def test_stable_structure_has_no_negative_frequency(self):
        t = 5000.0
        structure = _structure([[0, 0, 0], [1, 0, 0], [2, 0, 0]],
                               [[False] * 3, [True, False, True], [False] * 3], [[0, 1], [1, 2]], [t, t])
        masses = np.zeros((3, 3))
        masses[1] = 1.0
        with warnings.catch_warnings():
            warnings.simplefilter("error")  # no warning expected
            results = main_dynamic_modal_analysis(structure, point_masses=masses)
        self.assertEqual(results.unstable_mode_count, 0)
        self.assertAlmostEqual(results.frequencies[0], np.sqrt(2 * t) / (2 * np.pi), places=9)


if __name__ == '__main__':
    unittest.main()
