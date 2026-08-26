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
# This fork (Muscle 2.0): added by Basile Payen (UCLouvain) with Claude (Anthropic).

# Acknowledgements:
# The kinetic-damping Dynamic Relaxation scheme (fictitious mass from the stiffness
# diagonal, explicit velocity/displacement time-stepping, kinetic-energy peak detection and
# reset) is a direct generalization, to 6 DOF/node, of Muscle's own axial DR solver
# (musclepy.solvers.dr.main) - see that module for the original 3-DOF version and its
# reference [1] Bel Hadj Ali N., Rhode-Barbarigos L., Smith I.F.C. (2011).

"""
musclepy.solvers.beam_dr - BEAM FE MODEL ONLY, not used by the axial DR/SRDR pipeline.

Explicit, mass-based kinetic-damping Dynamic Relaxation (KDR) for the Beam FE model: 6
DOF/node (3 translations + 3 rotations), with a fictitious rotational "mass" (moment of
inertia analogue) computed from the rotational stiffness diagonal exactly like Muscle's own
DR computes fictitious translational mass - the same Gershgorin-diagonal rule works for
either kind of DOF since it is a purely numerical relaxation device, not a physical mass.

Unlike musclepy.solvers.beam_fem (Newton-Raphson: assembles and solves a global tangent
stiffness system every iteration), this solver never assembles a stiffness matrix beyond
reading its diagonal, and never performs a linear solve - matching the whole point of DR
methods (robust and cheap for large/ill-conditioned systems). Both solvers share the exact
same element-level physics (musclepy.solvers.beam_common) and therefore solve the exact same
underlying equilibrium equations - two independent numerical methods that should agree on
their converged answer for the same inputs (see the Beam DR implementation plan's
cross-validation approach).

No file in Muscle's own codebase or in the reference thesis (Simon Lizee, "Numerical models
for tensegrity", UCLouvain 2026) implements this: the thesis's DR_implicite.py is, despite
its name and its quaternion-based rotation handling, NOT an explicit method - it assembles
and solves a full tangent stiffness system every iteration (np.linalg.solve), only wrapping
the Newton increment in DR-styled relaxation coefficients. Its one genuinely explicit,
mass-based DR (DR_hybride/DR_truss.py) is strictly 3-DOF/translational. This module is a
genuine generalization, not a port.
"""

import numpy as np
from musclepy.solvers.beam_fem.py_beam_nodes import PyBeamNodes
from musclepy.solvers.beam_fem.py_beam_elements import PyBeamElements
from musclepy.solvers.beam_dr.py_config_beam_dr import PyConfigBeamDR
from musclepy.solvers.beam_common import corotational_frames, assemble_global_system


def solve(nodes: PyBeamNodes, elements: PyBeamElements, config: PyConfigBeamDR = None):
    """
    Solve the Beam FE model to equilibrium via explicit kinetic-damping Dynamic Relaxation.

    Args:
        nodes: PyBeamNodes instance (immutable inputs: initial_coordinates, dof; mutable:
            loads - the loads to apply)
        elements: PyBeamElements instance
        config: PyConfigBeamDR instance, default settings used if None

    Returns:
        (nodes, elements, config): same instances, mutated in place with the solved state -
        see musclepy.solvers.beam_fem.main.solve for the exact same result shape (this
        solver's output is form-compatible with Beam FEM's, since both share the same
        PyBeamNodes/PyBeamElements result fields).
    """
    assert isinstance(nodes, PyBeamNodes), "nodes must be a PyBeamNodes instance"
    assert isinstance(elements, PyBeamElements), "elements must be a PyBeamElements instance"
    if config is None:
        config = PyConfigBeamDR()
    else:
        assert isinstance(config, PyConfigBeamDR), "config must be a PyConfigBeamDR instance"

    n_dof = nodes.count * 6
    dt = config.dt

    AE = elements.area * elements.youngs
    EI = elements.youngs * elements.inertia
    GJ = elements.shear_modulus * elements.torsion_constant
    L0 = elements.free_length

    r_C, assemblage = corotational_frames(nodes, elements)

    dof_mask = nodes.dof.reshape(-1)  # True = free
    fixed_dof = np.where(~dof_mask)[0]
    P_ref = nodes.loads.reshape(-1).astype(float)

    U = np.zeros(n_dof)
    V = np.zeros(n_dof)
    KE_prev = 0.0

    converged = False
    residual_norm = np.inf
    p_bsc = None
    P_r = np.zeros(n_dof)

    for _ in range(config.max_time_step):
        P_r, K_str, p_bsc = assemble_global_system(U, r_C, assemblage, AE, EI, GJ, L0, elements)

        R = P_ref - P_r
        R[fixed_dof] = 0

        residual_norm = np.linalg.norm(R)
        if residual_norm <= config.tol_force:
            converged = True
            break

        # Fictitious mass per DOF from the stiffness diagonal (Gershgorin-type rule) - same
        # rule as musclepy.solvers.dr.main._compute_current_masses (eq. 19 of ref [1] in
        # musclepy.solvers.dr's own __init__.py), generalized here to 6 DOF/node: the same
        # formula applies unchanged to rotational DOF, since it is a numerical relaxation
        # device tied to local stiffness magnitude, not a physical mass.
        M = 2.0 * dt ** 2 * np.diag(K_str) * config.mass_ampl_factor
        M = np.maximum(M, config.min_mass)
        M[fixed_dof] = config.huge_mass

        V_incr = dt * R / M
        if np.allclose(V, 0.0, atol=1e-12):
            V_incr = V_incr / 2.0
        V_next = V + V_incr

        KE_next = 0.5 * np.sum(M[dof_mask] * V_next[dof_mask] ** 2)

        U_correction = np.zeros(n_dof)
        if KE_next <= KE_prev:
            # Kinetic energy peak just passed: parabolic position correction, reset velocity
            # and KE - the textbook KDR peak-reset, identical to musclepy.solvers.dr.main.
            U_correction = -1.5 * dt * V_next + 0.5 * dt ** 2 * R / M
            V_next = np.zeros(n_dof)
            KE_next = 0.0
            config.n_ke_reset += 1
            if config.n_ke_reset >= config.max_ke_reset:
                break

        U = U + dt * V_next + U_correction
        V = V_next
        KE_prev = KE_next
        config.n_time_step += 1

    config.converged = converged
    config.residual = residual_norm

    _write_results(nodes, elements, U, P_ref, P_r, fixed_dof, p_bsc)

    return nodes, elements, config


def _write_results(nodes: PyBeamNodes, elements: PyBeamElements, U, P, P_r, fixed_dof, p_bsc):
    """Same result shape as musclepy.solvers.beam_fem.main._write_results."""
    nodes.displacements = U.reshape(nodes.count, 6)
    nodes.loads = P.reshape(nodes.count, 6)
    nodes.resisting_forces = P_r.reshape(nodes.count, 6)

    reactions = np.zeros_like(U)
    reactions[fixed_dof] = P_r[fixed_dof] - P[fixed_dof]
    nodes.reactions = reactions.reshape(nodes.count, 6)

    elements.tension = p_bsc[:, 0].copy()
    elements.moment_start = p_bsc[:, [3, 1]].copy()  # [My, Mz] at start
    elements.moment_end = p_bsc[:, [4, 2]].copy()  # [My, Mz] at end
    elements.torsion = p_bsc[:, 5].copy()
