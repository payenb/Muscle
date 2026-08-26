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
musclepy.solvers.beam_dr - BEAM FE MODEL ONLY, not used by the axial DR/SRDR pipeline.

Explicit kinetic-damping Dynamic Relaxation for the Beam FE model: 6 DOF/node, generalizing
musclepy.solvers.dr's own explicit DR to rotational DOF. See main.py's module docstring for
the full formulation.
"""

from .main import solve
from .py_config_beam_dr import PyConfigBeamDR

__all__ = ['solve', 'PyConfigBeamDR']
