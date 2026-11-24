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

"""Validation against the published values of the experimental tensegrity simplex:

Feron J., Rhode-Barbarigos L., Latteur P. (2023). Experimental testing of a tensegrity simplex:
self-stress implementation and static loading. Journal of Structural Engineering 149(7), 04023073.
DOI 10.1061/JSENDH.STENG-11517. "pub. N" = page 04023073-N of the published version.

Data copied from the repository tensegrite-vs-treillis (src/tvt/v02_cables.py, section 8, and its
reading note of the article). Tolerances follow the printed precision of each value; where the
computation of Muscle differs from the printed one by more than that, the gap is stated.
"""

import math
import unittest
import numpy as np
import musclepy as mp
from musclepy import PyNodes, PyElements, PyTruss
from musclepy.solvers.dr.py_config_dr import PyConfigDR

# Node coordinates printed in figure 1(b), pub. 3 [mm]
PRINTED_NODES_MM = np.array([[0.0, -2043.8, 0.0], [0.0, 0.0, 0.0], [1770.0, -1021.9, 0.0],
                             [590.0, -2201.9, 1950.0], [-431.9, -431.9, 1950.0], [1611.9, -431.9, 1950.0]])
STRUTS, BOTTOM, TOP, VERTICAL = [0, 1, 2], [3, 4, 5], [6, 7, 8], [9, 10, 11]
END_NODES = np.array([[0, 5], [1, 3], [2, 4],      # struts S
                      [0, 1], [1, 2], [2, 0],      # bottom cables CB
                      [3, 4], [4, 5], [5, 3],      # top cables CT
                      [0, 3], [1, 4], [2, 5]])     # "vertical" cables CV
# Supports, pub. 6: bottom nodes fixed in z; node 0 also in x (rail along y); node 1 in x and y
DOF = np.array([[False, True, False], [False, False, False], [True, True, False]] + [[True] * 3] * 3)
SELF_WEIGHT_N = (45.7, 41.6)  # per bottom node, per top node, pub. 7
# Self-stress level giving struts at -1.50 kN under self-weight (pub. 8), computed once with Muscle by
# bisection on the level of the self-stress state (dynamic relaxation)
LEVEL_FOR_1500N_UNDER_SELF_WEIGHT = 1355.76  # [N]


def exact_nodes():
    """[m] Nodes rebuilt from the dimensions of the article (equilateral triangles of side 2043.8 mm,
    top turned by 30 deg and raised by 1950 mm), as in tensegrite-vs-treillis: with the printed
    coordinates (rounded to 0.1 mm) the self-stress state only exists to 1e-5."""
    centre = PRINTED_NODES_MM[:3, :2].mean(axis=0)
    radius = 2043.8 / math.sqrt(3.0)
    angles = np.radians([-120.0, 120.0, 0.0])
    out = []
    for z, turn in ((0.0, 0.0), (1950.0, math.radians(30.0))):
        for a in angles:
            out.append([centre[0] + radius * math.cos(a + turn), centre[1] + radius * math.sin(a + turn), z])
    return np.array(out) * 1e-3


def simplex():
    """The tested simplex, table 1 (pub. 5): struts 364.4 mm², E = 70 390 MPa (compression only);
    cables 50.3 mm², E = 71 750 MPa (CB, CT) and 72 190 MPa (CV) (tension only). Not prestressed."""
    youngs = np.zeros((12, 2))
    youngs[STRUTS, 0] = 70390e6
    youngs[BOTTOM + TOP, 1] = 71750e6
    youngs[VERTICAL, 1] = 72190e6
    area = np.r_[np.full(3, 364.4e-6), np.full(9, 50.3e-6)]
    nodes = PyNodes(initial_coordinates=exact_nodes(), dof=DOF)
    elements = PyElements(nodes=nodes, type=np.r_[-np.ones(3), np.ones(9)].astype(int),
                          end_nodes=END_NODES, area=area, youngs=youngs)
    return PyTruss(nodes, elements)


def self_stress_mode(structure):
    """Self-stress state normalised as in table 2: struts at -1."""
    vs = mp.main_singular_value_decomposition(structure).Vs[:, 0]
    return vs / -vs[0]


def loads(load_per_top_node=0.0):
    """[N] self-weight plus a vertical load on each top node (test 1, pub. 7)."""
    out = np.zeros((6, 3))
    out[:3, 2] = -SELF_WEIGHT_N[0]
    out[3:, 2] = -SELF_WEIGHT_N[1] - load_per_top_node
    return out


def height_and_twist(coordinates):
    """Mean height of the top triangle above the bottom one [mm], and its twist alpha [deg]."""
    bottom, top = coordinates[:3], coordinates[3:]
    height = top[:, 2].mean() - bottom[:, 2].mean()
    cb, ct = bottom[:, :2].mean(axis=0), top[:, :2].mean(axis=0)
    angles = []
    for i, j in ((0, 0), (1, 1), (2, 2)):  # bottom node i and the top node of its vertical cable
        vb, vt = bottom[i, :2] - cb, top[j, :2] - ct
        angles.append(math.degrees(math.atan2(vb[0] * vt[1] - vb[1] * vt[0], vb @ vt)))
    return height * 1e3, float(np.mean(angles))


def state_of_test_1(load_per_top_node):
    """Equilibrium of test 1: level 1.5 kN under self-weight, plus the load on each top node."""
    structure = simplex()
    prestressed = mp.structure_with_tension(structure, LEVEL_FOR_1500N_UNDER_SELF_WEIGHT * self_stress_mode(structure))
    config = PyConfigDR(zero_residual_rtol=1e-6, zero_residual_atol=1e-4, max_time_step=20000, max_ke_reset=2000)
    return mp.main_dynamic_relaxation(prestressed, loads(load_per_top_node), np.zeros(12), config)


class TestFeron2023Simplex(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.structure = simplex()
        cls.mode = self_stress_mode(cls.structure)

    def test_exact_nodes_match_the_printed_ones(self):
        np.testing.assert_allclose(exact_nodes() * 1e3, PRINTED_NODES_MM, atol=0.05)

    def test_table_1_lengths_and_axial_stiffness(self):
        """Lengths 2999.8 / 2043.8 / 2043.4 mm and EA/L 8551 / 1765 / 1776 N/mm (table 1, pub. 5)."""
        el = self.structure.elements
        L = el.current_length
        np.testing.assert_allclose(L[STRUTS] * 1e3, 2999.8, atol=0.05)
        np.testing.assert_allclose(L[BOTTOM + TOP] * 1e3, 2043.8, atol=0.05)
        np.testing.assert_allclose(L[VERTICAL] * 1e3, 2043.4, atol=0.05)
        ea_over_l = np.max(el.youngs, axis=1) * el.area / L / 1e3  # [N/mm]
        np.testing.assert_allclose(ea_over_l[STRUTS], 8551, rtol=1e-3)
        np.testing.assert_allclose(ea_over_l[BOTTOM + TOP], 1765, rtol=1e-3)
        np.testing.assert_allclose(ea_over_l[VERTICAL], 1776, rtol=1e-3)

    def test_table_2_self_stress_state(self):
        """s = 1, m = 1 (pub. 6); mode: struts -1, CB and CT 0.393, CV 0.681 (table 2, pub. 7);
        a_gov = 6 kN / 0.681 = 8.81 kN, governed by the vertical cables (pub. 6)."""
        svd = mp.main_singular_value_decomposition(self.structure)
        self.assertEqual((svd.s, svd.m), (1, 1))
        np.testing.assert_allclose(self.mode[STRUTS], -1.0, atol=1e-12)
        np.testing.assert_allclose(self.mode[BOTTOM + TOP], 0.393, atol=5e-4)
        np.testing.assert_allclose(self.mode[VERTICAL], 0.681, atol=5e-4)
        self.assertAlmostEqual(6.0 / self.mode[VERTICAL[0]], 8.81, delta=0.005)

    def test_table_2_sensitivities(self):
        """Linear sensitivity of the level a (table 2, pub. 7): +602 N per mm of strut lengthening,
        +237 / +410 N per mm of CB-CT / CV shortening. Linear displacement method on the simplex
        prestressed at a = 1.5 kN.

        Each element is changed in the direction that keeps it taut (strut lengthened, cable
        shortened): the linear displacement method picks the modulus of each element in the
        fictitious state where the nodes are fixed, so a cable lengthened by more than its elastic
        elongation (0.33 mm here) would count as slack."""
        prestressed = mp.structure_with_tension(self.structure, 1500.0 * self.mode)
        for index, dl0, expected in ((STRUTS[0], +1e-3, 602.0), (BOTTOM[0], -1e-3, 237.0),
                                     (TOP[0], -1e-3, 237.0), (VERTICAL[0], -1e-3, 410.0)):
            dl = np.zeros(12)
            dl[index] = dl0
            result = mp.main_linear_displacement_method(prestressed, np.zeros(18), dl)
            delta_level = -np.mean(result.elements.tension[STRUTS] - prestressed.elements.tension[STRUTS])
            self.assertAlmostEqual(delta_level, expected, delta=1.0, msg=f"element {index}")

    def test_table_3_elongations_for_1kN(self):
        """Imposed elongations of the self-stress scenario for Δa = 1 kN (table 3, pub. 8): struts
        +0.117 mm, CB and CT -0.223 mm, CV -0.384 mm (printed to 3 decimals, hence 0.5 %)."""
        prestressed = mp.structure_with_tension(self.structure, 1500.0 * self.mode)
        dl = np.zeros(12)
        dl[STRUTS], dl[BOTTOM + TOP], dl[VERTICAL] = 0.117e-3, -0.223e-3, -0.384e-3
        result = mp.main_linear_displacement_method(prestressed, np.zeros(18), dl)
        delta_level = -np.mean(result.elements.tension[STRUTS] - prestressed.elements.tension[STRUTS])
        self.assertAlmostEqual(delta_level, 1000.0, delta=5.0)

    def test_self_stress_is_prestress_stable(self):
        prestressed = mp.structure_with_tension(self.structure, 1500.0 * self.mode)
        self.assertEqual(mp.main_prestress_stability(prestressed).status, "stable")
        self.assertEqual(mp.main_tangent_stiffness_stability(prestressed).status, "stable")

    def test_figure_2b_start_of_test_1(self):
        """Start of the tests, with self-weight (fig. 2(b), pub. 4; pub. 8): struts -1.50 kN,
        CV 0.98 kN, horizontal cables 0.59 kN, H = 1946.0 mm, alpha = 30.7 deg.
        Muscle gives H = 1945.5 mm: 0.5 mm (3e-4) below the printed value."""
        state = state_of_test_1(0.0)
        t = state.elements.tension / 1e3
        np.testing.assert_allclose(t[STRUTS], -1.50, atol=0.005)
        np.testing.assert_allclose(t[VERTICAL], 0.98, atol=0.005)
        np.testing.assert_allclose(t[BOTTOM + TOP], 0.59, atol=0.005)
        height, alpha = height_and_twist(state.nodes.coordinates)
        self.assertAlmostEqual(height, 1946.0, delta=1.0)
        self.assertAlmostEqual(alpha, 30.7, delta=0.05)
        self.assertEqual(mp.main_tangent_stiffness_stability(state).status, "stable")

    def test_test_1_at_1300N_per_node(self):
        """1.3 kN per top node (pub. 10): H = 1918 mm, alpha = 35.0 deg (Muscle: 1917.6 mm, 35.04 deg)."""
        height, alpha = height_and_twist(state_of_test_1(1300.0).nodes.coordinates)
        self.assertAlmostEqual(height, 1918.0, delta=1.0)
        self.assertAlmostEqual(alpha, 35.0, delta=0.1)

    def test_test_1_at_2100N_per_node(self):
        """2.1 kN per top node, computed by dynamic relaxation in the article (fig. 2(c), pub. 4;
        pub. 10): struts -10.80 kN, CV 5.1 kN, horizontal cables 4.4 kN, H = 1911.5 mm,
        alpha = 36.0 deg. Muscle gives struts -10.87 kN (0.7 % more compression), H = 1910.9 mm."""
        state = state_of_test_1(2100.0)
        t = state.elements.tension / 1e3
        np.testing.assert_allclose(t[STRUTS], -10.80, rtol=0.01)
        np.testing.assert_allclose(t[VERTICAL], 5.1, atol=0.05)
        np.testing.assert_allclose(t[BOTTOM + TOP], 4.4, atol=0.05)
        height, alpha = height_and_twist(state.nodes.coordinates)
        self.assertAlmostEqual(height, 1911.5, delta=1.0)
        self.assertAlmostEqual(alpha, 36.0, delta=0.1)


if __name__ == '__main__':
    unittest.main()
