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
musclepy.solvers.stability - Is an equilibrium state stable?

For a conservative system (dead loads, elastic members), an equilibrium state is stable when
the tangent stiffness K_t = K_material + K_geometric, restricted to the free degrees of freedom,
is positive definite (Guest 2006, "The stiffness of prestressed frameworks: a unifying approach").
An equilibrium found by a Newton-type method (displacement methods) is only a zero of the
residual and can be unstable; the dynamic relaxation, which dissipates kinetic energy, ends in a
stable state.

Three outcomes, with a relative tolerance tol on the largest diagonal term of K_t (scale):

    stable      smallest eigenvalue >  tol * scale
    critical    |smallest eigenvalue| <= tol * scale: a (near) zero eigenvalue, i.e. an
                infinitesimal mechanism, or a limit or bifurcation point
    unstable    smallest eigenvalue < -tol * scale

Slack cables carry no force and add no material stiffness (current Young's modulus = 0 in
compression). Limit: where a cable is exactly at zero tension the energy is only C1 and K_t is
the one-sided stiffness of the slack side.
"""

import numpy as np
from musclepy.femodel.pytruss import PyTruss
from musclepy.utils.matrix_calculations import compute_tangent_stiffness_matrix
from musclepy.solvers.stability.py_results_stability import PyResultsStability

STABILITY_TOL = 1e-10  # relative to the largest diagonal term of the tangent stiffness


def main_tangent_stiffness_stability(structure: PyTruss, tol: float = STABILITY_TOL) -> PyResultsStability:
    """Stability of the current state of a structure: smallest eigenvalue of its tangent stiffness.

    Args:
        structure: PyTruss in an equilibrium state (coordinates, free lengths and tensions consistent,
                   e.g. the output of a solver)
        tol: [-] relative tolerance, see the module docstring

    Returns:
        PyResultsStability: status, smallest eigenvalue [N/m] and its mode (critical_mode), number
        of negative eigenvalues
    """
    assert isinstance(structure, PyTruss), "structure must be a PyTruss instance"
    free = structure.nodes.dof.reshape(-1)
    K = compute_tangent_stiffness_matrix(structure)[np.ix_(free, free)]
    scale = _scale(K)
    return _classify(K, np.eye(K.shape[0]), scale, tol, free)


def _scale(K_free: np.ndarray) -> float:
    """[N/m] largest diagonal term of the tangent stiffness on the free DOF (1 if there is none)."""
    if K_free.size == 0:
        return 1.0
    scale = float(np.max(np.abs(np.diag(K_free))))
    return scale if scale > 0 else 1.0


def _classify(K_reduced: np.ndarray, basis: np.ndarray, scale: float, tol: float,
              free: np.ndarray) -> PyResultsStability:
    """Classify the symmetric matrix K_reduced, expressed in the orthonormal basis `basis`
    (shape (n_free, k)) of a subspace of the free DOF.

    The critical mode is returned on all the 3*nodes_count DOF (0 on fixed DOF).
    """
    if K_reduced.shape[0] == 0:
        return PyResultsStability("stable", np.inf, scale, 0, np.array([]), tol)

    values, vectors = np.linalg.eigh(0.5 * (K_reduced + K_reduced.T))
    min_eigenvalue = values[0]
    if min_eigenvalue > tol * scale:
        status = "stable"
    elif min_eigenvalue >= -tol * scale:
        status = "critical"
    else:
        status = "unstable"
    negative_count = int(np.sum(values < -tol * scale))

    mode_free = basis @ vectors[:, 0]
    mode_free = mode_free / np.linalg.norm(mode_free)
    if mode_free[np.argmax(np.abs(mode_free))] < 0:
        mode_free = -mode_free  # sign convention: largest component positive
    critical_mode = np.zeros(free.size)
    critical_mode[free] = mode_free
    return PyResultsStability(status, min_eigenvalue, scale, negative_count, critical_mode, tol)
