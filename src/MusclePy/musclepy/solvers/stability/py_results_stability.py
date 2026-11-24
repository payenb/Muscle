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

import numpy as np


class PyResultsStability:
    """Stability of one equilibrium state, from the eigenvalues of a symmetric stiffness matrix.

    Attributes:
        status: "stable", "critical" or "unstable" (see musclepy.solvers.stability.main)
        min_eigenvalue: [N/m] smallest eigenvalue of the tested matrix
        scale: [N/m] reference stiffness of the relative tolerance: largest diagonal term, on the
               free DOF, of the tangent stiffness (or of the geometric stiffness for the
               Pellegrino-Calladine test)
        negative_count: [-] number of eigenvalues below -tol * scale
        critical_mode: [m, normalised] shape (3*nodes_count,) - eigenvector of the smallest
               eigenvalue, as nodal displacements (0 on fixed DOF), unit norm, largest component
               positive. Empty array if the tested space has no dimension (e.g. no mechanism).
        tol: [-] relative tolerance used for the classification
        mechanism_count: [-] number of infinitesimal mechanisms (Pellegrino-Calladine test only, None
               otherwise)
    """

    def __init__(self, status: str, min_eigenvalue: float, scale: float, negative_count: int,
                 critical_mode: np.ndarray, tol: float):
        assert status in ("stable", "critical", "unstable"), f"unknown status {status}"
        self.status = status
        self.min_eigenvalue = float(min_eigenvalue)
        self.scale = float(scale)
        self.negative_count = int(negative_count)
        self.critical_mode = np.asarray(critical_mode, dtype=float)
        self.tol = float(tol)
        self.mechanism_count = None

    @property
    def is_stable(self) -> bool:
        """True if the state is stable (all eigenvalues above tol * scale)."""
        return self.status == "stable"

    @property
    def min_eigenvalue_normalized(self) -> float:
        """[-] smallest eigenvalue divided by the scale."""
        return self.min_eigenvalue / self.scale

    def __repr__(self):
        return (f"PyResultsStability(status={self.status!r}, min_eigenvalue={self.min_eigenvalue:.6g} N/m, "
                f"scale={self.scale:.6g} N/m, negative_count={self.negative_count})")
