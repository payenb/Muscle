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

class PyConfigSRDR:
    """
    Configuration parameters for the Stiffness-Reduction Dynamic Relaxation (SRDR) method.

    Controls the outer iterative loop (Fig. 6 of [1]) that updates the compression Young's
    modulus of struts to account for the loss of axial stiffness induced by their initial
    geometric imperfection (out-of-straightness), and re-runs the (unmodified) Dynamic
    Relaxation solver until the axial force in every strut has converged.

    References:
    [1] Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric
        Imperfections on the Internal Forces Distribution in Tensegrity Structures",
        Journal of Structural Engineering, 2026, 152(5): 04026039.
        DOI: 10.1061/JSENDH.STENG-15452.
    """

    def __init__(self, force_tol=50.0, max_outer_iterations=100, warm_start=True):
        """
        Initialize the Stiffness-Reduction Dynamic Relaxation configuration.

        Args:
            force_tol: [N] - Convergence tolerance on the maximum strut axial force change
                       between two successive outer iterations (Fig. 6, box 6)
            max_outer_iterations: Maximum number of outer (stiffness-reduction) iterations
                       before giving up
            warm_start: If True, each outer DR run starts from the previous outer iteration's
                       converged geometry/state (faster in practice); if False, it always
                       restarts from the original input structure, as literally depicted by
                       the repeated "Perform the DR method" boxes of Fig. 6.
        """
        self.force_tol = force_tol if force_tol > 0 else 50.0  # [N]
        self.max_outer_iterations = max_outer_iterations if max_outer_iterations > 0 else 100
        self.warm_start = warm_start

        # Initialize counters, to be returned to the user for information regarding the solver performances.
        self.n_outer_iterations = 0
        self.converged = False
