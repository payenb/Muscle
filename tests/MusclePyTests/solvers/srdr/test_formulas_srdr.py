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

"""
Reference values for the aluminum strut used throughout the article are taken from:
Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric Imperfections
on the Internal Forces Distribution in Tensegrity Structures", J. Struct. Eng., 2026,
152(5): 04026039. Table 1, and the worked example on p. 3-4 (lambda=146.2, Ncr=11.8kN,
lambda_rel=2.2, fy*A=58.3kN).
"""

import unittest
import numpy as np
from musclepy.solvers.srdr import formulas


class TestFormulasSRDR(unittest.TestCase):
    def setUp(self):
        """Aluminum strut from Table 1 of the article: hollow circular section,
        outside diameter 60mm, thickness 2mm, E=70390 MPa, L0=2999.8mm. SI units, like musclepy."""
        E = 70390e6  # [Pa]
        D = 60e-3  # [m]
        t = 2e-3  # [m]
        self.L0 = 2.9998  # [m]
        Ro, Ri = D / 2, D / 2 - t
        self.area = np.pi * (Ro ** 2 - Ri ** 2)
        self.inertia = np.pi / 4 * (Ro ** 4 - Ri ** 4)
        self.youngs = E

    def test_slenderness_matches_article(self):
        """Article states lambda = 146.2 for this strut."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        self.assertAlmostEqual(lam, 146.2, delta=0.1)

    def test_euler_load_matches_article(self):
        """Article states Ncr = 11.8 kN for this strut."""
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        self.assertAlmostEqual(ncr / 1000, 11.8, delta=0.05)

    def test_stiffness_reduction_factor_perfect_strut_is_always_one(self):
        """A perfectly straight strut (imperfection_ratio = 0) keeps Kred/K = 1 for any N."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        N = np.array([0.0, 1000.0, 5000.0, 10000.0])
        ratio = formulas.stiffness_reduction_factor(N, ncr, np.zeros_like(N), lam)
        np.testing.assert_allclose(ratio, 1.0)

    def test_stiffness_reduction_factor_bounded(self):
        """Kred/K must stay within (0, 1] over the whole valid domain N in (0, Ncr)."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        e0_L0 = 1 / 300
        N_values = np.linspace(1.0, 0.95 * ncr, 50)

        ratios = formulas.stiffness_reduction_factor(N_values, np.full_like(N_values, ncr),
                                                       np.full_like(N_values, e0_L0), np.full_like(N_values, lam))

        self.assertTrue(np.all(ratios > 0))
        self.assertTrue(np.all(ratios <= 1.0))

    def test_stiffness_reduction_factor_is_globally_monotonic(self):
        """Kred/K(N) must be monotonically decreasing over the full domain N in (0, Ncr),
        as claimed by the article (Fig. 6, box 8: the bisection scheme "exploits the
        monotonic relationship between axial force and stiffness"). An earlier version of
        this formula (dropping the Lbow,0 ~= L0 approximation's correction term, see
        chord_length_ratio()) broke this property near N=0; this test guards against
        regressing to that broken behavior."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        e0_L0 = 1 / 300

        N_values = np.linspace(1.0, 0.95 * ncr, 500)
        ratios = formulas.stiffness_reduction_factor(N_values, np.full_like(N_values, ncr),
                                                       np.full_like(N_values, e0_L0), np.full_like(N_values, lam))

        self.assertTrue(np.all(np.diff(ratios) < 0), "Kred/K should strictly decrease as N increases")

    def test_stiffness_reduction_factor_at_half_ncr(self):
        """Pinned regression value at N = 0.5*Ncr, e0/L0 = 1/300 (computed and verified by hand
        from the corrected Eq. 10-11, see chord_length_ratio(); not an independently published
        article value)."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        ratio = formulas.stiffness_reduction_factor(0.5 * ncr, ncr, 1 / 300, lam)
        self.assertAlmostEqual(ratio, 0.7372, places=3)

    def test_stiffness_reduction_factor_stays_finite_at_or_beyond_ncr(self):
        """N reaching or exceeding Ncr (e.g. an overloaded strut, or an intermediate,
        not-yet-converged state of the outer bisection loop) must not produce NaN/inf: the
        (1 - N/Ncr) term in chord_length_ratio()'s discriminant would otherwise blow up,
        and a NaN stiffness ratio silently propagates into the inner DR run's Young's
        modulus, which never converges (NaN comparisons are always False) and spins through
        the full outer x inner iteration budget instead of failing fast - this hung
        Grasshopper for several minutes under a heavy load in practice."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        N_values = np.array([ncr, 1.5 * ncr, 10.0 * ncr])

        ratios = formulas.stiffness_reduction_factor(N_values, np.full_like(N_values, ncr),
                                                       np.full_like(N_values, 1 / 300), np.full_like(N_values, lam))

        self.assertTrue(np.all(np.isfinite(ratios)))
        self.assertTrue(np.all(ratios > 0))
        self.assertTrue(np.all(ratios <= 1.0))

    def test_amplified_deflection_matches_hand_calc_at_half_ncr(self):
        """Pinned regression value at N = 0.5*Ncr: e_max = e0/(1-N/Ncr) = 2*e0."""
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        e0 = self.L0 / 300
        e_max = formulas.amplified_deflection(0.5 * ncr, ncr, e0)
        self.assertAlmostEqual(e_max, 2 * e0, places=6)

    def test_amplified_deflection_equals_e0_at_zero_force(self):
        """No axial load -> no amplification: e_max = e0."""
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        e0 = self.L0 / 300
        self.assertAlmostEqual(formulas.amplified_deflection(0.0, ncr, e0), e0, places=6)

    def test_amplified_deflection_stays_finite_at_or_beyond_ncr(self):
        """N reaching or exceeding Ncr must not produce inf/NaN (N/Ncr is capped at 0.999)."""
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        e0 = self.L0 / 300
        N_values = np.array([ncr, 1.5 * ncr, 10.0 * ncr])
        e_max = formulas.amplified_deflection(N_values, np.full_like(N_values, ncr), np.full_like(N_values, e0))

        self.assertTrue(np.all(np.isfinite(e_max)))
        self.assertTrue(np.all(e_max >= e0))

    def test_amplified_deflection_is_monotonically_increasing(self):
        """e_max(N) must increase monotonically with N, consistent with a strut bowing out
        more as it approaches its buckling load."""
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        e0 = self.L0 / 300
        N_values = np.linspace(0.0, 0.99 * ncr, 200)
        e_max = formulas.amplified_deflection(N_values, np.full_like(N_values, ncr), np.full_like(N_values, e0))
        self.assertTrue(np.all(np.diff(e_max) > 0))

    def test_stiffness_reduction_factor_stays_finite_and_below_one_at_zero_force(self):
        """A strut with an initial bow is never quite as stiff as a perfectly straight one,
        even completely unloaded: Kred/K(N=0) should be a finite value strictly below 1
        (not 0 - see the correction note in chord_length_ratio() - and not exactly 1 either,
        since the strut is not perfectly straight)."""
        lam = formulas.slenderness(self.L0, self.area, self.inertia)
        ncr = formulas.euler_load(self.youngs, self.inertia, self.L0)
        ratio = formulas.stiffness_reduction_factor(0.0, ncr, 1 / 300, lam)
        self.assertGreater(ratio, 0.5)
        self.assertLess(ratio, 1.0)


if __name__ == '__main__':
    unittest.main()
