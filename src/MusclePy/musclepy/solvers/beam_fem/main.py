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

"""
musclepy.solvers.beam_fem - BEAM FE MODEL ONLY, not used by the axial DR/SRDR pipeline.

Corotational Newton-Raphson beam-truss FE solver: 6 DOF/node (3 translations + 3 rotations),
2-node beam-column elements with a "basic system" reduction (axial + 2x bending + torsion,
after removing the 6 rigid-body modes) and per-end hinge releases that let the same element
represent either a rigid/semi-rigid beam or a pin-jointed truss member.

This is a direct algorithmic port of Simon Lizee's thesis ("Numerical models for tensegrity",
UCLouvain, 2026) Load_control.py, cross-validated against it during development. Ported with
two deliberate v1 scope cuts (see the Beam FE model implementation plan):
- no pretension/prestress
- no tension-only (cable) nonlinearity - every element resists both tension and compression
Both can be added later without changing the element formulation below.

The element-level mechanics (corotational frame, nonlinear compatibility matrix, basic-system
stiffness, geometric/P-Delta stiffness) live in musclepy.solvers.beam_common, shared with
musclepy.solvers.beam_dr (an explicit kinetic-damping DR solver using the exact same element
physics with a different outer iteration scheme). See that module's docstring for the
corotational reference-frame convention (r_C fixed at the undeformed geometry) and why it's
valid for the moderate-rotation beam-column problems Muscle targets.
"""

import numpy as np
from musclepy.solvers.beam_fem.py_beam_nodes import PyBeamNodes
from musclepy.solvers.beam_fem.py_beam_elements import PyBeamElements
from musclepy.solvers.beam_fem.py_config_beam import PyConfigBeam
from musclepy.solvers.beam_common import corotational_frames, assemble_global_system


def solve(nodes: PyBeamNodes, elements: PyBeamElements, config: PyConfigBeam = None):
    """
    Solve the Beam FE model to equilibrium via Newton-Raphson iteration.

    Args:
        nodes: PyBeamNodes instance (immutable inputs: initial_coordinates, dof; mutable:
            loads - the loads to apply)
        elements: PyBeamElements instance
        config: PyConfigBeam instance, default settings used if None

    Returns:
        (nodes, elements, config): same instances, mutated in place with the solved state
        (nodes.displacements/reactions/resisting_forces, elements.tension/moment_start/
        moment_end/torsion, config.converged/n_iteration/residual).
    """
    assert isinstance(nodes, PyBeamNodes), "nodes must be a PyBeamNodes instance"
    assert isinstance(elements, PyBeamElements), "elements must be a PyBeamElements instance"
    if config is None:
        config = PyConfigBeam()
    else:
        assert isinstance(config, PyConfigBeam), "config must be a PyConfigBeam instance"

    n_dof = nodes.count * 6

    AE = elements.area * elements.youngs
    EI = elements.youngs * elements.inertia
    GJ = elements.shear_modulus * elements.torsion_constant
    L0 = elements.free_length

    r_C, assemblage = corotational_frames(nodes, elements)

    fixed_dof = np.where(~nodes.dof.reshape(-1))[0]
    P_ref = nodes.loads.reshape(-1).astype(float)

    U = np.zeros(n_dof)
    p_bsc = None

    factor = np.linspace(1.0 / config.nb_step, 1.0, config.nb_step)
    converged = False
    residual_norm = np.inf

    for step in range(config.nb_step):
        P = factor[step] * P_ref

        for iteration in range(config.max_iteration):
            P_r, K_str, p_bsc = assemble_global_system(U, r_C, assemblage, AE, EI, GJ, L0, elements)

            R = P - P_r

            K_str[fixed_dof, :] = 0
            K_str[:, fixed_dof] = 0
            K_str[fixed_dof, fixed_dof] = 1
            R[fixed_dof] = 0

            residual_norm = np.linalg.norm(R)
            if residual_norm <= config.tol_force:
                converged = True
                break

            U += np.linalg.solve(K_str, R)

        if not converged:
            break

    config.converged = converged
    config.n_iteration = iteration + 1
    config.residual = residual_norm

    _write_results(nodes, elements, U, P_ref, P_r, fixed_dof, p_bsc)

    return nodes, elements, config


def _write_results(nodes: PyBeamNodes, elements: PyBeamElements, U, P, P_r, fixed_dof, p_bsc):
    """Populate nodes/elements with the solved state - see PyBeamNodes/PyBeamElements for
    field meanings."""
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
