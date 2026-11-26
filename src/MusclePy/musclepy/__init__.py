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
MusclePy is a Python package for structural analysis developed by Université catholique de Louvain (UCLouvain). It focuses on the design, analysis, and optimization of tensegrity, tension-based, and truss-like structures.

Units: every input and output of musclepy is SI - lengths and displacements [m], areas [m²],
second moments of area [m⁴], Young's moduli and stresses [Pa], forces [N], moments [N.m],
masses [kg], frequencies [Hz]. musclepy never converts units. The conversion to the units
shown to the user (kN, MPa, mm...) is done once, by the Grasshopper components.
"""

# Define version first
__version__ = "2.0.1"

# Import subpackages 
from . import femodel

# Expose key classes at package level
from .femodel.pynodes import PyNodes
from .femodel.pyelements import PyElements
from .femodel.pytruss import PyTruss
from .femodel.prestress import free_length_from_tension, structure_with_tension

# import subpackages solvers
from . import solvers

# Expose solver functions - these imports should be after solvers is imported
from .solvers.test.test_script import main as test_script_main
from .solvers.svd.main import main_singular_value_decomposition
from .solvers.selfstress.modes import localize_self_stress_modes
from .solvers.dm.linear_dm import main_linear_displacement_method
from .solvers.dm.nonlinear_dm import main_nonlinear_displacement_method
from .solvers.dr.main import main_dynamic_relaxation
from .solvers.svd.py_results_svd import PyResultsSVD
from .solvers.dr.py_config_dr import PyConfigDR
from .solvers.srdr.main import main_stiffness_reduction_dr
from .solvers.srdr.py_config_srdr import PyConfigSRDR
from .solvers.beam_fem.main import solve as main_beam_fem
from .solvers.beam_fem.py_config_beam import PyConfigBeam
from .solvers.beam_fem.py_beam_nodes import PyBeamNodes
from .solvers.beam_fem.py_beam_elements import PyBeamElements
from .solvers.beam_dr.main import solve as main_beam_dr
from .solvers.beam_dr.py_config_beam_dr import PyConfigBeamDR
from .solvers.dynamic.py_results_dynamic import PyResultsDynamic
from .solvers.dynamic.main import main_dynamic_modal_analysis
from .solvers.stability.main import main_tangent_stiffness_stability, main_prestress_stability
from .solvers.stability.py_results_stability import PyResultsStability



__all__ = [
    'femodel',
    'solvers',
    'test_script_main',
    'PyNodes',
    'PyElements',
    'PyTruss',
    'free_length_from_tension',
    'structure_with_tension',
    'PyResultsSVD',
    'PyConfigDR',
    'PyResultsDynamic',
    'main_singular_value_decomposition',
    'localize_self_stress_modes',
    'main_linear_displacement_method',
    'main_nonlinear_displacement_method',
    'main_dynamic_relaxation',
    'main_stiffness_reduction_dr',
    'PyConfigSRDR',
    'main_beam_fem',
    'PyConfigBeam',
    'PyBeamNodes',
    'PyBeamElements',
    'main_beam_dr',
    'PyConfigBeamDR',
    'main_dynamic_modal_analysis',
    'main_tangent_stiffness_stability',
    'main_prestress_stability',
    'PyResultsStability'
]