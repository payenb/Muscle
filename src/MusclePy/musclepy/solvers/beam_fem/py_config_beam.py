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


class PyConfigBeam:
    """BEAM FE MODEL ONLY - configuration for the Newton-Raphson corotational Beam FE solver
    (musclepy.solvers.beam_fem), analogous to PyConfigDR/PyConfigSRDR for the DR/SRDR solvers.
    """

    def __init__(self, tol_force=1e-5, max_iteration=200, nb_step=1):
        """
        Args:
            tol_force: [N or N.m] - residual norm below which equilibrium is considered reached
            max_iteration: maximum Newton-Raphson iterations per load step
            nb_step: number of load steps (linear ramp from 0 to full load) - use more than 1
                for strongly nonlinear cases where a single full-load step fails to converge
        """
        self.tol_force = tol_force if tol_force > 0 else 1e-5
        self.max_iteration = max_iteration if max_iteration > 0 else 200
        self.nb_step = nb_step if nb_step > 0 else 1

        # Returned to the user for information regarding the solver's performance.
        self.converged = False
        self.n_iteration = 0
        self.residual = None
