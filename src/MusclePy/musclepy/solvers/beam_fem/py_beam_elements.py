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
from musclepy.solvers.beam_fem.py_beam_nodes import PyBeamNodes
from musclepy.solvers.beam_fem._marshal_utils import to_1d_array, to_2d_array


class PyBeamElements:
    """BEAM FE MODEL ONLY - not used by the axial DR/SRDR pipeline (musclepy.femodel.PyElements).

    A 2-node, 6-DOF-per-node beam-column element, carrying real bending/shear/torsion
    stiffness - unlike PyElements' purely axial (pin-jointed) elements. Per-end hinge
    releases (hinge_start/hinge_end) let the same element represent either a genuine
    rigid/semi-rigid beam or a pin-jointed truss member (both ends released), matching
    MuscleApp.ViewModel.BeamElement's HingeStart/HingeEnd.

    Scope simplifications for v1 (see the Beam FE model implementation plan):
    - A single Young's modulus per element (not the bilinear tension/compression pair
      PyElements supports) - matches MuscleApp.ViewModel.BeamElement using Material.Ec only.
    - No tension-only (cable) nonlinearity - every BeamElement resists both tension and
      compression.
    - No pretension/prestress.
    All units SI throughout (m, Pa, N, N.m, rad) - see PyBeamNodes docstring for why.
    """

    def __init__(self, nodes: PyBeamNodes, end_nodes, area, youngs, inertia,
                 torsion_constant, shear_modulus, free_length=None,
                 hinge_start=None, hinge_end=None, orientation=None):
        """
        Args:
            nodes: PyBeamNodes instance
            end_nodes: [-] shape (n,2) int - node indices of each element's 2 ends
            area: [m²] shape (n,)
            youngs: [Pa] shape (n,) - single Young's modulus per element
            inertia: [m4] shape (n,) - symmetric bending inertia (Iyy=Izz), from CS.Inertia
            torsion_constant: [m4] shape (n,) - Saint-Venant torsion constant, from CS.J
            shear_modulus: [Pa] shape (n,) - from BilinearMaterial.G
            free_length: [m] shape (n,) - default: current distance between end nodes
            hinge_start: [-] shape (n,) bool - True releases both bending DOF at the start node
            hinge_end: [-] shape (n,) bool - True releases both bending DOF at the end node
            orientation: [-] shape (n,3) - reference vector for cross-section orientation,
                row of NaN means "automatic" (world Z, or world Y if colinear with the
                element axis) - matches BeamElement.Orientation=null.
        """
        if not isinstance(nodes, PyBeamNodes):
            raise TypeError("nodes must be a PyBeamNodes instance")
        self.nodes = nodes

        self._end_nodes = to_2d_array(end_nodes, 2, int)
        self._count = self._end_nodes.shape[0]

        self._area = self._as_1d(area, "area")
        self._youngs = self._as_1d(youngs, "youngs")
        self._inertia = self._as_1d(inertia, "inertia")
        self._torsion_constant = self._as_1d(torsion_constant, "torsion_constant")
        self._shear_modulus = self._as_1d(shear_modulus, "shear_modulus")

        self._hinge_start = np.zeros(self._count, dtype=bool) if hinge_start is None else to_1d_array(hinge_start, bool)
        self._hinge_end = np.zeros(self._count, dtype=bool) if hinge_end is None else to_1d_array(hinge_end, bool)

        self._orientation = np.full((self._count, 3), np.nan) if orientation is None else to_2d_array(orientation, 3, float)
        if self._orientation.shape != (self._count, 3):
            raise ValueError(f"orientation must have shape ({self._count},3), got {self._orientation.shape}")

        self.free_length = self._as_1d(free_length, "free_length") if free_length is not None else self._current_length()

        # Results, populated by musclepy.solvers.beam_fem.main.solve()
        self.tension = np.zeros(self._count, dtype=float)  # [N]
        self.moment_start = np.zeros((self._count, 2), dtype=float)  # [N.m] [My, Mz] at start node
        self.moment_end = np.zeros((self._count, 2), dtype=float)  # [N.m] [My, Mz] at end node
        self.torsion = np.zeros(self._count, dtype=float)  # [N.m]

    def _as_1d(self, arr, name) -> np.ndarray:
        arr = to_1d_array(arr, float)
        if arr.shape != (self._count,):
            raise ValueError(f"{name} must have shape ({self._count},), got {arr.shape}")
        return arr

    def _current_length(self) -> np.ndarray:
        coords = self.nodes.coordinates
        n0 = coords[self._end_nodes[:, 0]]
        n1 = coords[self._end_nodes[:, 1]]
        return np.linalg.norm(n1 - n0, axis=1)

    @property
    def count(self) -> int:
        return self._count

    @property
    def end_nodes(self) -> np.ndarray:
        return self._end_nodes

    @property
    def area(self) -> np.ndarray:
        return self._area

    @property
    def youngs(self) -> np.ndarray:
        return self._youngs

    @property
    def inertia(self) -> np.ndarray:
        return self._inertia

    @property
    def torsion_constant(self) -> np.ndarray:
        return self._torsion_constant

    @property
    def shear_modulus(self) -> np.ndarray:
        return self._shear_modulus

    @property
    def hinge_start(self) -> np.ndarray:
        return self._hinge_start

    @property
    def hinge_end(self) -> np.ndarray:
        return self._hinge_end

    @property
    def orientation(self) -> np.ndarray:
        return self._orientation
