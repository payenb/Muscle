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

"""
musclepy.solvers.srdr - Stiffness-Reduction Dynamic Relaxation solver

Accounts for the loss of axial stiffness of struts due to their initial geometric
imperfection (out-of-straightness), by wrapping the existing (unmodified) Dynamic
Relaxation solver (musclepy.solvers.dr) in an outer iterative loop that updates the
compression Young's modulus of each strut based on its current axial force.

References:
[1] Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric
    Imperfections on the Internal Forces Distribution in Tensegrity Structures",
    Journal of Structural Engineering, 2026, 152(5): 04026039.
    DOI: 10.1061/JSENDH.STENG-15452.
"""

from .main import main_stiffness_reduction_dr, DEFAULT_IMPERFECTION_RATIO
from .py_config_srdr import PyConfigSRDR
from . import formulas

__all__ = ['main_stiffness_reduction_dr', 'DEFAULT_IMPERFECTION_RATIO', 'PyConfigSRDR', 'formulas']
