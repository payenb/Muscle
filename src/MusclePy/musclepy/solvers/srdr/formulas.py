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

"""
musclepy.solvers.srdr.formulas - Analytical stiffness reduction for imperfect struts

Implements the closed-form expressions for the loss of axial stiffness of a strut
with an initial geometric imperfection (out-of-straightness), assumed to follow a
half-sine shape, as derived in:

[1] Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric
    Imperfections on the Internal Forces Distribution in Tensegrity Structures",
    Journal of Structural Engineering, 2026, 152(5): 04026039.
    DOI: 10.1061/JSENDH.STENG-15452.

Units: unlike the rest of MusclePy (which mixes mm^2 for area with meters for
lengths), the functions in this module are self-consistent and expect ALL lengths
(L0, e0) in [mm], areas in [mm^2], inertia in [mm^4], Young's modulus in [MPa],
and forces in [N]. Callers that source lengths from PyElements (which stores
free_length in meters) must convert to mm before calling these functions.
"""

import numpy as np


def slenderness(L0: np.ndarray, area: np.ndarray, inertia: np.ndarray) -> np.ndarray:
    """[-] Strut slenderness ratio: lambda = L0 * sqrt(A/I).

    Args:
        L0: [mm] Initial chord length between strut supports
        area: [mm^2] Cross-section area
        inertia: [mm^4] Second moment of area (moment of inertia)

    Returns:
        [-] Slenderness ratio lambda
    """
    return L0 * np.sqrt(area / inertia)


def euler_load(youngs: np.ndarray, inertia: np.ndarray, L0: np.ndarray) -> np.ndarray:
    """[N] Euler critical buckling load: Ncr = pi^2 * E * I / L0^2.

    Args:
        youngs: [MPa] Young's modulus (compression)
        inertia: [mm^4] Second moment of area (moment of inertia)
        L0: [mm] Initial chord length between strut supports

    Returns:
        [N] Euler critical load Ncr
    """
    return np.pi ** 2 * youngs * inertia / L0 ** 2


def chord_length_ratio(N: np.ndarray, ncr: np.ndarray, imperfection_ratio: np.ndarray,
                        lam: np.ndarray) -> np.ndarray:
    """[-] L/L0: ratio between the current and initial chord length.

    Positive root of the quadratic form obtained by combining the elastic
    shortening of the strut with the second-order amplification of its
    initial bow shape. Only valid for 0 <= N < ncr.

    Correction w.r.t. Eq. 10 of [1]: the published equation drops the initial bow
    length correction term (approximating Lbow,0 ~= L0 in Eq. 5 of [1], "since
    e0 << L0"), which is a valid approximation while the strut carries significant
    load but breaks down as N -> 0 (it wrongly forces L/L0 -> 1 and hence Kred/K -> 0
    at zero load, and makes Kred/K non-monotonic in N - contradicting the monotonic
    force/stiffness relationship the box-8 bisection of Fig. 6 relies on). Keeping the
    Lbow,0 = L0*(1 + pi^2/4*(e0/L0)^2) term exact instead adds a + pi^2/8*(e0/L0)^2
    contribution to the linear coefficient below (to first order in the - always small -
    (e0/L0)^2), restoring both a finite, physically meaningful stiffness at N=0 and
    global monotonicity in N.

    Args:
        N: [N] Axial compressive force (positive for compression)
        ncr: [N] Euler critical load
        imperfection_ratio: [-] e0/L0
        lam: [-] Slenderness ratio

    Returns:
        [-] L/L0
    """
    N_ncr = N / ncr
    N_EA = (np.pi ** 2 / lam ** 2) * N_ncr  # Eq. 9 of [1]
    linear_coeff = 0.5 * (1 - N_EA) * (1 + (np.pi ** 2 / 4) * imperfection_ratio ** 2)
    discriminant = linear_coeff ** 2 - (np.pi ** 2 / 4) * imperfection_ratio ** 2 / (1 - N_ncr) ** 2
    return linear_coeff + np.sqrt(discriminant)


def stiffness_reduction_factor(N: np.ndarray, ncr: np.ndarray, imperfection_ratio: np.ndarray,
                                lam: np.ndarray) -> np.ndarray:
    """[-] Kred/K: ratio between the reduced (imperfect) and perfect axial stiffness (Eq. 11 of [1]).

    Only meaningful for a strut currently in compression (N > 0, N < ncr); callers
    are responsible for filtering out cables and struts currently in tension before
    calling this function (a perfectly straight strut, imperfection_ratio == 0,
    always returns 1 regardless of N).

    Kred/K(N) is globally monotonically decreasing from N=0 to N=ncr for a given
    imperfection_ratio > 0: it starts below 1 already at N=0 (a strut with an initial
    bow is never quite as stiff as a perfectly straight one, even unloaded) and tends
    to 0 as N -> ncr (buckling). See chord_length_ratio() for the correction applied
    with respect to Eq. 10-11 of [1] that is needed to get this right at low N.

    Args:
        N: [N] Axial compressive force (positive for compression)
        ncr: [N] Euler critical load
        imperfection_ratio: [-] e0/L0
        lam: [-] Slenderness ratio

    Returns:
        [-] Kred/K, capped at 1 (an imperfect strut can never be stiffer than a perfect one)
    """
    N = np.asarray(N, dtype=float)
    ncr = np.asarray(ncr, dtype=float)
    imperfection_ratio = np.asarray(imperfection_ratio, dtype=float)
    lam = np.asarray(lam, dtype=float)

    # Use a tiny dummy imperfection where imperfection_ratio == 0 to avoid a 0/0 division;
    # the result on that branch is discarded and forced to 1 below. A value close to 0 (as
    # opposed to e.g. 1.0) keeps the discriminant well-behaved for any N in [0, ncr).
    safe_imperfection_ratio = np.where(imperfection_ratio > 0, imperfection_ratio, 1e-6)

    # At N = 0, both the numerator (N/Ncr) and the denominator (1-L/L0) of the ratio below
    # tend to 0 together (Kred/K has a finite, nonzero limit there - see this function's
    # docstring), which is a genuine 0/0 in floating point even though the true function is
    # continuous. Nudge N away from exactly 0 (relative to ncr) to evaluate that limit
    # numerically instead, consistently in both the numerator and chord_length_ratio() below;
    # this is a no-op for any N that isn't already extremely close to 0.
    safe_N = np.maximum(N, 1e-6 * ncr)

    L_L0 = chord_length_ratio(safe_N, ncr, safe_imperfection_ratio, lam)
    N_ncr = safe_N / ncr
    ratio = (np.pi ** 2 / lam ** 2) * N_ncr / (1 - L_L0)
    ratio = np.where(imperfection_ratio > 0, ratio, 1.0)

    return np.minimum(ratio, 1.0)
