// Muscle

// Copyright <2015-2025> <Université catholique de Louvain (UCLouvain)>

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// List of the contributors to the development of Muscle: see NOTICE file.
// Description and complete License: see NOTICE file.
// ------------------------------------------------------------------------------------------------------------

namespace MuscleCore.Solvers
{
    /// <summary>
    /// Configuration parameters for the Stiffness-Reduction Dynamic Relaxation (SRDR) method.
    /// Mirrors musclepy.solvers.srdr.py_config_srdr.PyConfigSRDR. Controls the outer iterative
    /// loop that updates the compression Young's modulus of struts to account for the loss of
    /// axial stiffness induced by their initial geometric imperfection (out-of-straightness),
    /// re-running the (unmodified) Dynamic Relaxation solver until the axial force in every
    /// strut has converged.
    /// </summary>
    public class CoreConfigSRDR
    {
        /// <summary>
        /// [N] - Convergence tolerance on the maximum strut axial force change between two
        /// successive outer iterations
        /// </summary>
        public double ForceTol { get; set; }

        /// <summary>
        /// Maximum number of outer (stiffness-reduction) iterations before giving up
        /// </summary>
        public int MaxOuterIterations { get; set; }

        /// <summary>
        /// If true, each outer DR run starts from the previous outer iteration's converged
        /// geometry/state (faster in practice); if false, it always restarts from the original
        /// input structure.
        /// </summary>
        public bool WarmStart { get; set; }

        /// <summary>
        /// Number of outer iterations performed (output parameter)
        /// </summary>
        public int NOuterIterations { get; set; }

        /// <summary>
        /// Whether the outer loop converged within MaxOuterIterations (output parameter)
        /// </summary>
        public bool Converged { get; set; }

        /// <summary>
        /// Default constructor with recommended default values
        /// </summary>
        public CoreConfigSRDR()
        {
            ForceTol = 50.0;
            MaxOuterIterations = 100;
            WarmStart = true;
            NOuterIterations = 0;
            Converged = false;
        }

        /// <summary>
        /// Full constructor with all input parameters
        /// </summary>
        /// <param name="forceTol">[N] - Convergence tolerance on the maximum strut axial force change</param>
        /// <param name="maxOuterIterations">Maximum number of outer iterations before giving up</param>
        /// <param name="warmStart">Whether each outer DR run warm-starts from the previous one</param>
        public CoreConfigSRDR(
            double forceTol = 50.0,
            int maxOuterIterations = 100,
            bool warmStart = true)
        {
            ForceTol = forceTol > 0 ? forceTol : 50.0;
            MaxOuterIterations = maxOuterIterations > 0 ? maxOuterIterations : 100;
            WarmStart = warmStart;
            NOuterIterations = 0;
            Converged = false;
        }
    }
}
