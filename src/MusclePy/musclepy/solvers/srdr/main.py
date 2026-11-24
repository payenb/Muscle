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
References:
[1] Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric
    Imperfections on the Internal Forces Distribution in Tensegrity Structures",
    Journal of Structural Engineering, 2026, 152(5): 04026039.
    DOI: 10.1061/JSENDH.STENG-15452.

Note: this is a Phase 1 implementation. The ultimate limit state check (Eq. 14 of
[1], first yield of a strut) is NOT implemented yet; it will be added in a later
phase, together with the yield strength (fy) and elastic section modulus (Wel)
inputs it requires.
"""

import numpy as np
from musclepy.femodel.pytruss import PyTruss
from musclepy.solvers.dr.main import main_dynamic_relaxation
from musclepy.solvers.dr.py_config_dr import PyConfigDR
from musclepy.solvers.dr.py_truss_dr import PyTrussDR
from musclepy.solvers.dr.py_elements_dr import PyElementsDR
from musclepy.solvers.srdr.py_config_srdr import PyConfigSRDR
from musclepy.solvers.srdr import formulas

DEFAULT_IMPERFECTION_RATIO = 1 / 300  # e0/L0, recommended by Eurocode 9 (CEN 2007), see [1]

# Max allowed gap between the applied Kred/K and the value implied by the force it produced,
# for the outer loop to be considered converged (see the ratio_delta check below). Not exposed
# as a PyConfigSRDR setting: this is an internal numerical safeguard, not a physically
# meaningful tolerance a user would tune (unlike force_tol).
STIFFNESS_RATIO_TOL = 1e-3


def main_stiffness_reduction_dr(structure: PyTruss, loads_increment: np.ndarray = None,
                                 free_length_variation: np.ndarray = None,
                                 dr_config: PyConfigDR = None, srdr_config: PyConfigSRDR = None,
                                 inertia: np.ndarray = None, imperfection: np.ndarray = None) -> PyTrussDR:
    """
    Perform the Stiffness-Reduction Dynamic Relaxation method (Fig. 6 of [1]) on the structure.

    Accounts for the loss of axial stiffness of struts due to their initial geometric
    imperfection (out-of-straightness): after a first DR run assuming perfectly straight
    struts (boxes 1-2), the compression Young's modulus of each strut is scaled by its
    stiffness reduction factor Kred/K (Eq. 11 of [1], boxes 3-4), computed from its current
    axial force, and the (unmodified) Dynamic Relaxation solver is re-run (box 5). This is
    repeated (boxes 6-7) until the axial force in every strut has converged, using a
    bisection scheme on Kred/K (box 8) to ensure fast, stable convergence despite the
    two-way coupling between force and stiffness.

    Args:
        structure: Initial structure state
        loads_increment: External load increments to apply
        free_length_variation: Free length variation to apply
        dr_config: PyConfigDR used as a template for every inner DR run (a fresh copy with
                   the same settings is created at each outer iteration, so its counters
                   are not meaningful here; see srdr_config.n_outer_iterations instead)
        srdr_config: Configuration for the outer Stiffness-Reduction loop
        inertia: [m^4] - shape (elements_count,) - Second moment of area of elements (struts)
        imperfection: [m] - shape (elements_count,) - Initial imperfection amplitude e0 of
                      elements (struts). Defaults to free_length * DEFAULT_IMPERFECTION_RATIO
                      (Eurocode 9's L0/300) for every element if not provided.

    Returns:
        PyTrussDR: The final structure state after Stiffness-Reduction Dynamic Relaxation

    Note:
        The srdr_config object is updated in-place with the number of outer iterations and
        the convergence status.
    """
    assert isinstance(structure, PyTruss), "Structure must be a PyTruss instance"
    if srdr_config is None:
        srdr_config = PyConfigSRDR()  # use default solver configuration
    else:
        assert isinstance(srdr_config, PyConfigSRDR), "srdr_config must be a PyConfigSRDR instance"

    # 1) Wrap elements as PyElementsDR with inertia/imperfection set (default e0 = L0/300,
    # a policy specific to this method - see DEFAULT_IMPERFECTION_RATIO)
    if imperfection is None:
        imperfection = structure.elements.free_length * DEFAULT_IMPERFECTION_RATIO
    srdr_elements = PyElementsDR(structure.elements, inertia=inertia, imperfection=imperfection)
    srdr_structure = PyTrussDR(structure.nodes, srdr_elements)

    is_strut = srdr_structure.elements.type == -1
    # Cast to float explicitly: youngs may be an integer array (e.g. built from Python ints
    # such as [[70390, 0], ...]), and every array derived from it below (ratios, N, ...) must
    # stay float, or in-place assignments of e.g. imperfection_ratio would silently truncate
    # to 0.
    original_compression_youngs = srdr_structure.elements.youngs[:, 0].astype(float).copy()

    # Precompute the (constant) geometric properties needed by the formulas. Everything is SI,
    # like the rest of musclepy: free_length [m], area [m^2], youngs [Pa], inertia [m^4].
    L0 = srdr_structure.elements.free_length
    area = srdr_structure.elements.area
    inertia = srdr_structure.elements.inertia
    imperfection_ratio = np.zeros_like(original_compression_youngs)
    imperfection_ratio[is_strut] = (srdr_structure.elements.imperfection[is_strut]
                                     / srdr_structure.elements.free_length[is_strut])

    lam = np.ones_like(original_compression_youngs)
    ncr = np.ones_like(original_compression_youngs)
    lam[is_strut] = formulas.slenderness(L0[is_strut], area[is_strut], inertia[is_strut])
    ncr[is_strut] = formulas.euler_load(
        original_compression_youngs[is_strut], inertia[is_strut], L0[is_strut]
    )

    # 2) First DR run assuming perfectly straight struts (Fig. 6, boxes 1-2)
    current_state = main_dynamic_relaxation(
        structure=srdr_structure,
        loads_increment=loads_increment,
        free_length_variation=free_length_variation,
        config=_fresh_dr_config(dr_config)
    )
    previous_tension = current_state.elements.tension.copy()
    applied_ratio = np.ones_like(original_compression_youngs)  # (Kred/K) actually applied to obtain current_state

    srdr_config.n_outer_iterations = 0
    srdr_config.converged = not np.any(is_strut)  # trivially converged if the structure has no strut

    while srdr_config.n_outer_iterations < srdr_config.max_outer_iterations and not srdr_config.converged:

        # 3-4/7) Compute the stiffness reduction factor Kred/K (Eq. 11) from the current strut forces
        N_compression = np.zeros_like(original_compression_youngs)
        N_compression[is_strut] = -current_state.elements.tension[is_strut]  # positive value in compression
        in_compression = is_strut & (N_compression > 0)

        raw_ratio = np.ones_like(original_compression_youngs)
        raw_ratio[in_compression] = formulas.stiffness_reduction_factor(
            N_compression[in_compression], ncr[in_compression],
            imperfection_ratio[in_compression], lam[in_compression]
        )

        # How far the ratio implied by the CURRENT force (raw_ratio) is from the ratio that was
        # actually applied to produce that force (applied_ratio, before this iteration's
        # update) - i.e. whether Kred/K is self-consistent yet. This is checked separately from
        # the force-based convergence below (step 6): in a statically determinate structure (or
        # any load path where a strut's force is largely fixed by equilibrium alone), the axial
        # force barely changes with stiffness, so the force-only check can appear "converged"
        # after a single bisection half-step even though applied_ratio is still far from its
        # fixed point - which would silently under-report the stiffness loss (and displacement).
        ratio_delta = np.max(np.abs(raw_ratio[in_compression] - applied_ratio[in_compression])) \
            if np.any(in_compression) else 0.0

        # 8) Bisection: average the freshly computed ratio with the one applied in the previous
        # iteration, exploiting the monotonic force/stiffness relationship for stable convergence.
        applied_ratio = 0.5 * (raw_ratio + applied_ratio)

        new_youngs = srdr_structure.elements.youngs.astype(float).copy()
        new_youngs[:, 0] = original_compression_youngs * applied_ratio

        # Choose the starting point of the next DR run (see PyConfigSRDR.warm_start)
        base_state = current_state if srdr_config.warm_start else srdr_structure
        next_elements = base_state.elements.copy_and_update(youngs=new_youngs)
        next_structure = PyTrussDR(base_state.nodes, next_elements)

        # 5) Re-run DR with the updated stiffness. When not warm-starting, the loads/free_length
        # variation must be re-applied since next_structure is rebuilt from the pristine input.
        if srdr_config.warm_start:
            current_state = main_dynamic_relaxation(structure=next_structure, config=_fresh_dr_config(dr_config))
        else:
            current_state = main_dynamic_relaxation(
                structure=next_structure,
                loads_increment=loads_increment,
                free_length_variation=free_length_variation,
                config=_fresh_dr_config(dr_config)
            )

        # 6) Convergence check: max axial force change over struts only, "for any strut"
        if np.any(is_strut):
            delta = np.max(np.abs(current_state.elements.tension[is_strut] - previous_tension[is_strut]))
        else:
            delta = 0.0
        previous_tension = current_state.elements.tension.copy()

        srdr_config.n_outer_iterations += 1
        # bool(...): comparisons against numpy scalars are numpy.bool (numpy 2.x), which
        # pythonnet's .As<bool>() on the C# side cannot convert to System.Boolean - cast to a
        # native Python bool before it crosses the Python.NET boundary. Both the force AND the
        # ratio must have settled (see ratio_delta above) - otherwise a structure whose strut
        # force is insensitive to that strut's stiffness (e.g. statically determinate) would
        # falsely report convergence after a single bisection half-step.
        srdr_config.converged = bool(delta <= srdr_config.force_tol and ratio_delta <= STIFFNESS_RATIO_TOL)

    # 9) Second-order bending moment M = N*e_max and amplified mid-span deflection e_max at the
    # converged state (Eq. 12-13 of [1]) - both 0 for cables and struts currently in tension,
    # where the beam-column imperfection model doesn't apply. See formulas.amplified_deflection.
    N_final = np.zeros_like(original_compression_youngs)
    N_final[is_strut] = -current_state.elements.tension[is_strut]
    in_compression_final = is_strut & (N_final > 0)

    e0 = srdr_structure.elements.imperfection  # [m]
    deflection = np.zeros_like(original_compression_youngs)  # [m]
    deflection[in_compression_final] = formulas.amplified_deflection(
        N_final[in_compression_final], ncr[in_compression_final], e0[in_compression_final]
    )
    moment = N_final * deflection  # [N.m]

    updated_elements = current_state.elements.copy_and_update(moment=moment, deflection=deflection)
    return PyTrussDR(current_state.nodes, updated_elements)


def _fresh_dr_config(template: PyConfigDR = None) -> PyConfigDR:
    """Build a new PyConfigDR with the same settings as `template` but freshly zeroed counters.

    A PyConfigDR mutates its n_time_step/n_ke_reset counters in place, so reusing the same
    instance across the repeated main_dynamic_relaxation() calls of the SRDR outer loop would
    make later inner DR runs start already "out of budget".
    """
    if template is None:
        return PyConfigDR()
    assert isinstance(template, PyConfigDR), "dr_config must be a PyConfigDR instance"
    return PyConfigDR(
        dt=template.dt,
        mass_ampl_factor=template.mass_ampl_factor,
        min_mass=template.min_mass,
        max_time_step=template.max_time_step,
        max_ke_reset=template.max_ke_reset,
        zero_residual_rtol=template.zero_residual_rtol,
        zero_residual_atol=template.zero_residual_atol,
    )
