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
// This fork (Muscle 2.0): added by Basile Payen (UCLouvain) with Claude (Anthropic).
// ------------------------------------------------------------------------------------------------------------

namespace MuscleCore.Solvers
{
    /// <summary>
    /// BEAM FE MODEL ONLY. Configuration for the explicit kinetic-damping Dynamic Relaxation
    /// Beam solver, mirroring musclepy.solvers.beam_dr.PyConfigBeamDR (DR-specific parameters
    /// shaped like CoreConfigDR, generalized to 6 DOF/node; convergence uses the same
    /// absolute residual-norm threshold as CoreConfigBeam for a consistent output shape
    /// between the two Beam solvers).
    /// </summary>
    public class CoreConfigBeamDR
    {
        /// <summary>[s] - Time step for the explicit time-incremental method</summary>
        public double Dt { get; set; }

        /// <summary>Amplification factor for the fictitious masses</summary>
        public double MassAmplFactor { get; set; }

        /// <summary>Minimum fictitious mass/inertia applied to each DOF if near-zero stiffness is detected</summary>
        public double MinMass { get; set; }

        /// <summary>Maximum number of time steps before termination</summary>
        public int MaxTimeStep { get; set; }

        /// <summary>Maximum number of kinetic energy resets before termination</summary>
        public int MaxKEResets { get; set; }

        /// <summary>[N or N.m] - residual norm below which equilibrium is considered reached</summary>
        public double ToleranceForce { get; set; }

        /// <summary>Whether the solver converged within MaxTimeStep (output parameter)</summary>
        public bool Converged { get; set; }

        /// <summary>Number of time steps performed (output parameter)</summary>
        public int NTimeStep { get; set; }

        /// <summary>Number of kinetic energy resets performed (output parameter)</summary>
        public int NKEReset { get; set; }

        /// <summary>Final residual norm (output parameter)</summary>
        public double Residual { get; set; }

        public CoreConfigBeamDR(double dt = 0.01, double massAmplFactor = 1.0, double minMass = 0.005,
            int maxTimeStep = 10000, int maxKEResets = 1000, double toleranceForce = 1e-5)
        {
            Dt = dt > 0 ? dt : 0.01;
            MassAmplFactor = massAmplFactor > 0 ? massAmplFactor : 1.0;
            MinMass = minMass > 0 ? minMass : 0.005;
            MaxTimeStep = maxTimeStep > 0 ? maxTimeStep : 10000;
            MaxKEResets = maxKEResets > 0 ? maxKEResets : 1000;
            ToleranceForce = toleranceForce > 0 ? toleranceForce : 1e-5;
            Converged = false;
            NTimeStep = 0;
            NKEReset = 0;
            Residual = 0;
        }
    }
}
