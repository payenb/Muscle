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
    /// BEAM FE MODEL ONLY. Configuration for the Newton-Raphson corotational Beam FE solver,
    /// mirroring musclepy.solvers.beam_fem.PyConfigBeam.
    /// </summary>
    public class CoreConfigBeam
    {
        /// <summary>[N or N.m] - residual norm below which equilibrium is considered reached</summary>
        public double ToleranceForce { get; set; }

        /// <summary>Maximum Newton-Raphson iterations per load step</summary>
        public int MaxIteration { get; set; }

        /// <summary>Number of load steps (linear ramp from 0 to full load)</summary>
        public int NbStep { get; set; }

        /// <summary>Whether the solver converged within MaxIteration (output parameter)</summary>
        public bool Converged { get; set; }

        /// <summary>Number of Newton-Raphson iterations performed on the last load step (output parameter)</summary>
        public int NIteration { get; set; }

        /// <summary>Final residual norm (output parameter)</summary>
        public double Residual { get; set; }

        public CoreConfigBeam(double toleranceForce = 1e-5, int maxIteration = 200, int nbStep = 1)
        {
            ToleranceForce = toleranceForce > 0 ? toleranceForce : 1e-5;
            MaxIteration = maxIteration > 0 ? maxIteration : 200;
            NbStep = nbStep > 0 ? nbStep : 1;
            Converged = false;
            NIteration = 0;
            Residual = 0;
        }
    }
}
