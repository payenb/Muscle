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
musclepy.femodel.prestress - Build a prestressed structure from the axial forces wanted.

Convention (Feron et al. 2019): the geometry given to Muscle is the geometry AFTER prestress. To
obtain the tensions t0 in that geometry, each element needs the free length

    L0 = L / (1 + t0 / EA)        (from t0 = EA/L0 * (L - L0))

where L is the current length and E the Young's modulus of the element in the state it will be in
(tension modulus if t0 > 0, compression modulus if t0 < 0).

Giving musclepy a `tension` without the matching `free_length` is not enough: the dynamic
relaxation recomputes the axial forces from the free lengths (by default the current lengths), and
the prestress disappears. PrestressScenario goes the other way (from free length variations to
forces).
"""

import numpy as np
from musclepy.femodel.pyelements import PyElements
from musclepy.femodel.pytruss import PyTruss


def free_length_from_tension(elements: PyElements, tension: np.ndarray) -> np.ndarray:
    """[m] Free lengths that give the axial forces `tension` in the current geometry.

    Args:
        elements: PyElements in the geometry where the forces are wanted
        tension: [N] - shape (elements_count,) - wanted axial forces (positive in tension)

    Returns:
        [m] - shape (elements_count,) - free lengths L0 = L / (1 + t / EA)

    Raises:
        ValueError: if an element has no stiffness in the wanted state (e.g. a cable asked to be in
            compression), or if the compression asked is at least EA (no positive free length exists)
    """
    assert isinstance(elements, PyElements), "elements must be a PyElements instance"
    tension = np.asarray(elements._check_and_reshape_array(tension, "tension"), dtype=float)
    youngs = np.asarray(elements.youngs, dtype=float)
    young = np.where(tension > 0, youngs[:, 1], youngs[:, 0])  # tension or compression modulus
    ea = young * np.asarray(elements.area, dtype=float)
    length = elements.current_length

    loaded = tension != 0
    no_stiffness = loaded & ~(ea > 0)
    if np.any(no_stiffness):
        raise ValueError(f"elements {np.flatnonzero(no_stiffness).tolist()} have no axial stiffness (E*A = 0) "
                         "in the state asked (e.g. a cable in compression): no free length gives that force")
    ratio = np.zeros_like(tension)
    ratio[loaded] = tension[loaded] / ea[loaded]
    if np.any(ratio <= -1):
        raise ValueError(f"elements {np.flatnonzero(ratio <= -1).tolist()}: a compression of EA or more "
                         "cannot be obtained with a positive free length")
    return length / (1 + ratio)


def structure_with_tension(structure: PyTruss, tension: np.ndarray) -> PyTruss:
    """Copy of the structure, prestressed by the axial forces `tension` in its current geometry.

    The free lengths are set by free_length_from_tension and the tensions to `tension`, so that the
    state is consistent and every solver (in particular the dynamic relaxation) keeps the prestress.
    If `tension` is a self-stress state (A t = 0), the copy is in equilibrium without any load.

    Args:
        structure: PyTruss in the geometry after prestress
        tension: [N] - shape (elements_count,) - wanted axial forces (positive in tension)

    Returns:
        PyTruss (same class as `structure`)
    """
    assert isinstance(structure, PyTruss), "structure must be a PyTruss instance"
    tension = np.asarray(structure.elements._check_and_reshape_array(tension, "tension"), dtype=float)
    free_length = free_length_from_tension(structure.elements, tension)
    nodes = structure.nodes.copy()
    elements = structure.elements.copy_and_update(nodes, free_length=free_length, tension=tension.copy())
    return structure._create_copy(nodes, elements)
