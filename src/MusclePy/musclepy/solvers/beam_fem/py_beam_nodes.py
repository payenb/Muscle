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

import numpy as np
from musclepy.solvers.beam_fem._marshal_utils import to_2d_array


class PyBeamNodes:
    """BEAM FE MODEL ONLY - not used by the axial DR/SRDR pipeline (musclepy.femodel.PyNodes).

    A node of the Beam FE model: 6 DOF per node (3 translations + 3 rotations), unlike
    PyNodes' 3 translational DOF only. DOF order per node is [Tx, Ty, Tz, Rx, Ry, Rz],
    matching MuscleApp.ViewModel.BeamNode and the thesis's Load_control.py convention.

    All units are SI throughout (m, Pa, N, N.m, rad), like the rest of musclepy (see the
    package docstring): no unit is ever converted here.

    Array inputs are coerced via musclepy.solvers.beam_fem._marshal_utils.to_2d_array, which
    handles both raw C# arrays (2D bool arrays in particular - see that module's docstring
    for why) and plain numpy arrays/nested sequences.
    """

    def __init__(self, initial_coordinates, dof, loads=None, displacements=None,
                 reactions=None, resisting_forces=None):
        """
        Args:
            initial_coordinates: [m] shape (n,3) - initial nodal positions
            dof: [-] shape (n,6) bool - True=free, False=fixed, order [Tx,Ty,Tz,Rx,Ry,Rz]
            loads: [N, N, N, N.m, N.m, N.m] shape (n,6) - external nodal loads, default 0
            displacements: [m, m, m, rad, rad, rad] shape (n,6) - default 0
            reactions: [N, N, N, N.m, N.m, N.m] shape (n,6) - default 0
            resisting_forces: [N, N, N, N.m, N.m, N.m] shape (n,6) - default 0
        """
        self._initial_coordinates = to_2d_array(initial_coordinates, 3, float)
        self._count = self._initial_coordinates.shape[0]

        self._dof = to_2d_array(dof, 6, bool)
        if self._dof.shape != (self._count, 6):
            raise ValueError(f"dof must have shape ({self._count},6), got {self._dof.shape}")

        self.loads = self._zeros_if_none(loads)
        self.displacements = self._zeros_if_none(displacements)
        self.reactions = self._zeros_if_none(reactions)
        self.resisting_forces = self._zeros_if_none(resisting_forces)

    def _zeros_if_none(self, arr) -> np.ndarray:
        if arr is None:
            return np.zeros((self._count, 6), dtype=float)
        arr = to_2d_array(arr, 6, float)
        if arr.shape != (self._count, 6):
            raise ValueError(f"expected shape ({self._count},6), got {arr.shape}")
        return arr

    @property
    def initial_coordinates(self) -> np.ndarray:
        """[m] shape (n,3) - immutable, set once from C#."""
        return self._initial_coordinates

    @property
    def dof(self) -> np.ndarray:
        """[-] shape (n,6) bool - immutable, set once from C#."""
        return self._dof

    @property
    def count(self) -> int:
        return self._count

    @property
    def fixations_count(self) -> int:
        return int(np.sum(~self._dof))

    @property
    def coordinates(self) -> np.ndarray:
        """[m] shape (n,3) - current translational position (initial + displacement)."""
        return self._initial_coordinates + self.displacements[:, :3]

    @property
    def residuals(self) -> np.ndarray:
        """[N, N, N, N.m, N.m, N.m] shape (n,6) - out-of-balance loads."""
        return self.loads + self.reactions - self.resisting_forces

    def copy(self) -> 'PyBeamNodes':
        return PyBeamNodes(
            initial_coordinates=self._initial_coordinates.copy(),
            dof=self._dof.copy(),
            loads=self.loads.copy(),
            displacements=self.displacements.copy(),
            reactions=self.reactions.copy(),
            resisting_forces=self.resisting_forces.copy(),
        )
