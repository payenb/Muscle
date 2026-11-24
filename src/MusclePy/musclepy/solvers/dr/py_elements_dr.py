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
from musclepy.femodel.pyelements import PyElements
from musclepy.femodel.pynodes import PyNodes
from musclepy.solvers.dr.py_nodes_dr import PyNodesDR


class PyElementsDR(PyElements):
    """
    Extension of PyElements with Dynamic Relaxation specific attributes.

    Attributes:
        All attributes from PyElements
        local_geometric_stiffness_matrices: List of local geometric stiffness matrices
        inertia: [m^4] - shape (elements_count,) - Second moment of area of elements
        imperfection: [m] - shape (elements_count,) - Initial imperfection amplitude e0 of elements
        moment: [N.m] - shape (elements_count,) - Second-order bending moment M = N*e_max
        deflection: [m] - shape (elements_count,) - Amplified mid-span deflection e_max

    Note: inertia/imperfection are physical properties of an element (like area), not
    algorithmic state - they live here (alongside area) rather than in a further
    solver-specific subclass so that any DR-family solver (e.g. musclepy.solvers.srdr,
    and future beam-based solvers) can use them without duplicating this plumbing. Plain
    DR simply ignores them (imperfection defaults to 0, i.e. no imperfection modeled).
    moment/deflection are results rather than inputs (set once musclepy.solvers.srdr
    converges; plain DR leaves them at their 0 default, same as an unset Tension) but live
    here for the same reason - so any DR-family solver can carry them without a further
    subclass.
    """

    def __init__(self, elements_or_nodes, type=None, end_nodes=None, area=None, youngs=None,
                 free_length=None, tension=None, inertia=None, imperfection=None,
                 moment=None, deflection=None):
        """Initialize a PyElementsDR instance.

        This constructor accepts either:
        1. A PyElements instance to convert to a PyElementsDR instance
        2. A PyNodes (or PyNodesDR) instance plus all parameters (legacy constructor)

        Args:
            elements_or_nodes: Either a PyElements instance or a PyNodes (or PyNodesDR) instance
            type: [-] - shape (elements_count,) - Type of elements (-1 for struts, 1 for cables)
            end_nodes: [-] - shape (elements_count, 2) - Indices of end nodes
            area: [m²] - shape (elements_count,) - Cross-section area of elements
            youngs: [Pa] - shape (elements_count, 2) - Young's moduli for compression and tension
            free_length: [m] - shape (elements_count,) - Free length of elements
            tension: [N] - shape (elements_count,) - Current tension in elements
            inertia: [m^4] - shape (elements_count,) - Second moment of area of elements.
                     Defaults to 0 (unused by plain DR).
            imperfection: [m] - shape (elements_count,) - Initial imperfection amplitude e0 of
                     elements. Defaults to 0 (unused by plain DR; musclepy.solvers.srdr applies
                     its own default of L0/300 when none is provided).
            moment: [N.m] - shape (elements_count,) - Second-order bending moment M = N*e_max.
                     Defaults to 0 (unused by plain DR; set by musclepy.solvers.srdr once
                     converged).
            deflection: [m] - shape (elements_count,) - Amplified mid-span deflection e_max.
                     Defaults to 0 (unused by plain DR; set by musclepy.solvers.srdr once
                     converged).
        """
        # Check if the first argument is a PyElements instance
        if isinstance(elements_or_nodes, PyElements):
            elements = elements_or_nodes

            # Assert that all other arguments are None when a PyElements instance is passed
            assert all(arg is None for arg in [type, end_nodes, area, youngs, free_length, tension]), \
                "When passing a PyElements instance, all other arguments must be None"

            # Call parent class constructor with the properties from the PyElements instance
            super().__init__(
                elements.nodes,
                elements.type,
                elements.end_nodes,
                elements.area,
                elements.youngs,
                elements.free_length,
                elements.tension
            )

            # If elements already carries inertia/imperfection/moment/deflection (i.e. it is
            # itself a PyElementsDR), preserve them unless explicitly overridden.
            if isinstance(elements, PyElementsDR):
                if inertia is None:
                    inertia = elements.inertia
                if imperfection is None:
                    imperfection = elements.imperfection
                if moment is None:
                    moment = elements.moment
                if deflection is None:
                    deflection = elements.deflection
        else:
            # Assert that the first argument is a PyNodes or PyNodesDR instance
            assert isinstance(elements_or_nodes, (PyNodes, PyNodesDR)), "First argument must be either a PyElements, PyNodes, or PyNodesDR instance"
            nodes = elements_or_nodes
            # Call parent class constructor with the provided parameters
            super().__init__(nodes, type, end_nodes, area, youngs, free_length, tension)

        # Initialize DR-specific attributes
        self._local_geometric_stiffness_matrices = []  # [N/m] - List(elements.count) of shape (6,6) matrices
        self._inertia = self._check_and_reshape_array(inertia, "inertia")
        self._imperfection = self._check_and_reshape_array(imperfection, "imperfection")
        self._moment = self._check_and_reshape_array(moment, "moment")
        self._deflection = self._check_and_reshape_array(deflection, "deflection")

    @property
    def local_geometric_stiffness_matrices(self) -> list:
        """Get the local geometric stiffness matrices."""
        return self._local_geometric_stiffness_matrices

    @local_geometric_stiffness_matrices.setter
    def local_geometric_stiffness_matrices(self, value):
        """Set the local geometric stiffness matrices."""
        self._local_geometric_stiffness_matrices = value

    @property
    def inertia(self) -> np.ndarray:
        """[m^4] - shape (elements_count,) - Second moment of area (moment of inertia)"""
        return self._inertia

    @property
    def imperfection(self) -> np.ndarray:
        """[m] - shape (elements_count,) - Initial imperfection amplitude e0"""
        return self._imperfection

    @property
    def moment(self) -> np.ndarray:
        """[N.m] - shape (elements_count,) - Second-order bending moment M = N*e_max"""
        return self._moment

    @property
    def deflection(self) -> np.ndarray:
        """[m] - shape (elements_count,) - Amplified mid-span deflection e_max"""
        return self._deflection

    def compute_current_state(self):
        """Compute the current state of the elements.
        
        This is a public function to be called once, in order to avoid recomputing 
        the local stiffness matrices at each constructor call.
        """
        self._compute_tension()
        self._compute_stiffness_matrices()
    
    def _compute_tension(self):
        """Compute the tension for each element based on elastic elongation and flexibility."""
        self._tension = self.elastic_elongation / self.flexibility
    
    def _compute_stiffness_matrices(self):
        """Update all local stiffness matrices of the elements based on the current state."""
        from musclepy.utils.matrix_calculations import compute_local_geometric_stiffness_matrices
        
        # Not used in DR
        # # local material stiffnesses : [N/m] - List(elements.count) of shape (6,6) matrices
        # self.local_material_stiffness_matrices = compute_local_material_stiffness_matrices(
        #     self.direction_cosines,
        #     self.flexibility
        # )
        
        # local geometric stiffnesses : [N/m] - List(elements.count) of shape (6,6) matrices
        self.local_geometric_stiffness_matrices = compute_local_geometric_stiffness_matrices(
            self.tension,
            self.current_length
        )
    
    def _create_copy(self, nodes, type, end_nodes, area, youngs, free_length, tension,
                      inertia=None, imperfection=None, moment=None, deflection=None):
        """Core copy method that creates a new instance of the appropriate class.

        This protected method is used by all copy methods to create a new instance.
        Child classes should override this method to return an instance of their own class.
        inertia/imperfection/moment/deflection default to the current values when not
        explicitly provided.

        Returns:
            A new instance of the appropriate class (PyElementsDR or a child class)
        """
        inertia = self._inertia if inertia is None else inertia
        imperfection = self._imperfection if imperfection is None else imperfection
        moment = self._moment if moment is None else moment
        deflection = self._deflection if deflection is None else deflection
        return self.__class__(
            nodes,
            type=type,
            end_nodes=end_nodes,
            area=area,
            youngs=youngs,
            free_length=free_length,
            tension=tension,
            inertia=inertia,
            imperfection=imperfection,
            moment=moment,
            deflection=deflection
        )

    def copy_and_update(self, nodes=None, free_length=None, tension=None, youngs=None,
                         moment=None, deflection=None) -> 'PyElementsDR':
        """Create a copy of this instance and update its state.

        Args:
            nodes: A PyNodes or PyNodesDR instance
            free_length: [m] - shape (elements_count,) - Free length of elements
            tension: [N] - shape (elements_count,) - Current tension in elements
            youngs: [Pa] - shape (elements_count, 2) - Young's moduli for compression and tension.
                    Needed by musclepy.solvers.srdr to apply the stiffness reduction factor
                    Kred/K to the compression Young's modulus between successive DR runs.
            moment: [N.m] - shape (elements_count,) - Second-order bending moment M = N*e_max.
                    Set by musclepy.solvers.srdr once converged.
            deflection: [m] - shape (elements_count,) - Amplified mid-span deflection e_max.
                    Set by musclepy.solvers.srdr once converged.

        Returns:
            A new instance with the updated state
        """
        # Use current values if not provided
        new_nodes = self.nodes if nodes is None else nodes
        free_length = self.free_length if free_length is None else free_length
        tension = self.tension if tension is None else tension
        youngs = self.youngs if youngs is None else youngs

        # Create a new instance with the updated state
        return self._create_copy(
            new_nodes,
            self.type.copy(),
            self.end_nodes.copy(),
            self.area.copy(),
            youngs,
            free_length,
            tension,
            moment=moment,
            deflection=deflection
        )
