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
BEAM FE MODEL ONLY. Shared corotational beam-column element mechanics, extracted from
musclepy.solvers.beam_fem.main so musclepy.solvers.beam_dr can reuse the exact same,
already-validated element physics (corotational frame, nonlinear compatibility matrix,
basic-system stiffness with hinge releases, geometric/P-Delta stiffness) with a different
outer iteration scheme (Newton-Raphson linear solve vs. explicit kinetic-damping DR).
Mirrors how musclepy.utils.matrix_calculations already serves as shared code for the
DR-family solvers.

See musclepy.solvers.beam_fem.main's module docstring for the full formulation background
(ported from Simon Lizee's thesis Load_control.py) and the corotational reference-frame
convention (r_C fixed at the undeformed geometry).
"""

import numpy as np

SOFT_RELEASE_STIFFNESS = 1e-8  # near-zero stiffness used for hinge-released basic DOF
REGULARIZATION = 1e-5  # tiny diagonal stiffness added to avoid singularity (matches thesis)


def corotational_frames(nodes, elements):
    """Local-to-global 12x12 rotation matrix and global-DOF index list per element, computed
    once from the undeformed geometry (see module docstring)."""
    n_elem = elements.count
    r_C = np.zeros((n_elem, 12, 12))
    assemblage = np.zeros((n_elem, 12), dtype=int)

    coords = nodes.initial_coordinates
    for i in range(n_elem):
        n0, n1 = elements.end_nodes[i]
        ex = coords[n1] - coords[n0]
        length = np.linalg.norm(ex)
        if length == 0:
            raise ValueError(f"Element {i} has zero length (nodes {n0} and {n1} coincide).")
        ex = ex / length

        v_ref = elements.orientation[i]
        if np.any(np.isnan(v_ref)):
            v_ref = np.array([0.0, 0.0, 1.0])

        ey = np.cross(ex, v_ref)
        if np.linalg.norm(ey) < 1e-8:
            v_ref = np.array([0.0, 1.0, 0.0])
            ey = np.cross(ex, v_ref)
        ey = ey / np.linalg.norm(ey)
        ez = np.cross(ey, ex)
        ey = -ey

        t = np.vstack((ex, ey, ez))
        T = np.zeros((12, 12))
        for j in range(4):
            T[j * 3:(j + 1) * 3, j * 3:(j + 1) * 3] = t
        r_C[i] = T

        assemblage[i] = [n0 * 6, n0 * 6 + 1, n0 * 6 + 2, n0 * 6 + 3, n0 * 6 + 4, n0 * 6 + 5,
                          n1 * 6, n1 * 6 + 1, n1 * 6 + 2, n1 * 6 + 3, n1 * 6 + 4, n1 * 6 + 5]

    return r_C, assemblage


def chord_rotation(U, r_C, assemblage, L0, n_elem):
    """Beta/gamma (chord rotation about local z/y) and current chord length l, per element -
    the corotational quantities that carry the large-displacement/rotation nonlinearity."""
    beta = np.zeros(n_elem)
    gamma = np.zeros(n_elem)
    l = np.zeros(n_elem)
    for i in range(n_elem):
        u_loc_i = r_C[i] @ U[assemblage[i]]
        delta_ulx = u_loc_i[6] - u_loc_i[0]
        delta_uly = u_loc_i[7] - u_loc_i[1]
        delta_ulz = u_loc_i[8] - u_loc_i[2]
        beta[i] = np.arctan2(delta_uly, L0[i] + delta_ulx)
        gamma[i] = np.arctan2(delta_ulz, L0[i] + delta_ulx)
        l[i] = np.sqrt((L0[i] + delta_ulx) ** 2 + delta_uly ** 2 + delta_ulz ** 2)
    return beta, gamma, l


def compatibility_matrix(beta, gamma, l):
    """Local-to-basic-system nonlinear compatibility matrix A (6x12) - exact port of
    Load_control.py's per-element matrix."""
    cB, sB, cG, sG = np.cos(beta), np.sin(beta), np.cos(gamma), np.sin(gamma)
    return np.array([
        [-cB * cG, -sB, -sG, 0, 0, 0, cB * cG, sB, sG, 0, 0, 0],
        [-sB / l, cB / l, 0, sG, 0, cG, sB / l, -cB / l, 0, 0, 0, 0],
        [-sB / l, cB / l, 0, 0, 0, 0, sB / l, -cB / l, 0, sG, 0, cG],
        [sG / l, 0, -cG / l, -sB, cB, 0, -sG / l, 0, cG / l, 0, 0, 0],
        [sG / l, 0, -cG / l, 0, 0, 0, -sG / l, 0, cG / l, -sB, cB, 0],
        [0, 0, 0, -cB * cG, -sB, sG, 0, 0, 0, cB * cG, sB, -sG],
    ])


def basic_stiffness_matrix(ae, ei, gj, l0, hinge_start, hinge_end):
    """6x6 basic-system stiffness: [N, Mz_start, Mz_end, My_start, My_end, T]. Hinge releases
    zero out both bending rows/cols at the released end (soft near-zero stiffness), exactly
    as Load_control.py's rotule_start/rotule_end."""
    k = np.array([
        [ae / l0, 0, 0, 0, 0, 0],
        [0, 4 * ei / l0, 2 * ei / l0, 0, 0, 0],
        [0, 2 * ei / l0, 4 * ei / l0, 0, 0, 0],
        [0, 0, 0, 4 * ei / l0, 2 * ei / l0, 0],
        [0, 0, 0, 2 * ei / l0, 4 * ei / l0, 0],
        [0, 0, 0, 0, 0, gj / l0],
    ])
    if hinge_start:
        k[1, :] = 0; k[:, 1] = 0; k[1, 1] = SOFT_RELEASE_STIFFNESS
        k[3, :] = 0; k[:, 3] = 0; k[3, 3] = SOFT_RELEASE_STIFFNESS
    if hinge_end:
        k[2, :] = 0; k[:, 2] = 0; k[2, 2] = SOFT_RELEASE_STIFFNESS
        k[4, :] = 0; k[:, 4] = 0; k[4, 4] = SOFT_RELEASE_STIFFNESS
    return k


def geometric_stiffness_matrix(beta, gamma, l):
    """12x12 local geometric (P-Delta) stiffness matrix, scaled by the axial force outside
    this function - exact port of Load_control.py's G1 matrix (transcribed term-by-term,
    NOT block-symmetric - the (0,2)/(2,0) sub-terms genuinely differ in sign in the
    reference, a real feature of this beta/gamma "basic system" parametrization, not a typo)."""
    cB, sB, cG, sG = np.cos(beta), np.sin(beta), np.cos(gamma), np.sin(gamma)
    return (1.0 / l) * np.array([
        [sB**2 + sG**2, -sB*cB, sG*cG, 0, 0, 0, -sB**2 - sG**2, sB*cB, sG*cG, 0, 0, 0],
        [-sB*cB, cB**2, 0, 0, 0, 0, sB*cB, -cB**2, 0, 0, 0, 0],
        [-sG*cG, 0, cG**2, 0, 0, 0, sG*cG, 0, -cG**2, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        [-sB**2 - sG**2, sB*cB, -sG*cG, 0, 0, 0, sB**2 + sG**2, -sB*cB, -sG*cG, 0, 0, 0],
        [sB*cB, -cB**2, 0, 0, 0, 0, -sB*cB, cB**2, 0, 0, 0, 0],
        [sG*cG, 0, -cG**2, 0, 0, 0, -sG*cG, 0, cG**2, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
    ])


def assemble_global_system(U, r_C, assemblage, AE, EI, GJ, L0, elements):
    """
    Compute the internal resisting force vector P_r, the global tangent stiffness matrix
    K_str (material + geometric stiffness, with REGULARIZATION already added), and the
    per-element basic forces p_bsc, at the current displacement state U.

    Shared by beam_fem (Newton-Raphson) and beam_dr (explicit KDR) - both need exactly this
    per-iteration assembly; they differ only in what they do with the result (Newton solves
    K \\ R for a displacement increment; DR uses diag(K) as a fictitious mass and R directly
    for an explicit velocity update - see beam_dr.main).

    Returns:
        P_r: shape (n_dof,) - internal resisting force vector
        K_str: shape (n_dof, n_dof) - global tangent stiffness matrix
        p_bsc: shape (n_elem, 6) - per-element basic-system forces [N, Mz_start, Mz_end,
            My_start, My_end, T]
    """
    n_elem = elements.count
    n_dof = len(U)

    beta, gamma, l = chord_rotation(U, r_C, assemblage, L0, n_elem)

    P_r = np.zeros(n_dof)
    K_str = np.zeros((n_dof, n_dof))
    p_bsc = np.zeros((n_elem, 6))

    for i in range(n_elem):
        u_glob_i = U[assemblage[i]]
        u_loc_i = r_C[i] @ u_glob_i

        A_i = compatibility_matrix(beta[i], gamma[i], l[i])

        u_bsc_i = np.zeros(6)
        u_bsc_i[0] = l[i] - L0[i]
        u_bsc_i[1] = u_loc_i[5] - beta[i]
        u_bsc_i[2] = u_loc_i[11] - beta[i]
        u_bsc_i[3] = u_loc_i[4] + gamma[i]
        u_bsc_i[4] = u_loc_i[10] + gamma[i]
        u_bsc_i[5] = u_loc_i[9] - u_loc_i[3]

        k_bsc = basic_stiffness_matrix(AE[i], EI[i], GJ[i], L0[i],
                                        elements.hinge_start[i], elements.hinge_end[i])

        p_bsc[i] = k_bsc @ u_bsc_i

        p_loc_i = A_i.T @ p_bsc[i]
        p_glob_i = r_C[i].T @ p_loc_i
        P_r[assemblage[i]] += p_glob_i

        G1_i = geometric_stiffness_matrix(beta[i], gamma[i], l[i])
        k_loc_i = A_i.T @ k_bsc @ A_i + p_bsc[i, 0] * G1_i
        k_glob_i = r_C[i].T @ k_loc_i @ r_C[i]
        K_str[np.ix_(assemblage[i], assemblage[i])] += k_glob_i

    K_str += np.eye(n_dof) * REGULARIZATION

    return P_r, K_str, p_bsc
