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

using System;
using System.Collections.Generic;
using MuscleApp.ViewModel;
using MuscleApp.Converters;
using MuscleCore.FEModel;
using MuscleCore.Solvers;

namespace MuscleApp.Solvers
{
    /// <summary>
    /// BEAM FE MODEL ONLY - a new, parallel solver, entirely independent of
    /// DynamicRelaxation/StiffnessReductionDynamicRelaxation AND of BeamFEM (Newton-Raphson).
    /// Explicit kinetic-damping Dynamic Relaxation for the Beam FE model. Reuses the exact
    /// same BeamStructureEncoder/BeamLoadEncoder/BeamStructureDecoder as MuscleApp.Solvers.
    /// BeamFEM (same physical model/data types, different solver).
    /// </summary>
    public static class BeamDR
    {
        /// <summary>
        /// Solve the Beam FE model to equilibrium via explicit kinetic-damping DR.
        /// </summary>
        public static BeamStructure? Solve(BeamStructure structure, IEnumerable<PointLoad> pointForces,
            IEnumerable<PointLoad> pointMoments, CoreConfigBeamDR config)
        {
            CoreBeamStructure coreStructure = BeamStructureEncoder.ToCore(structure);

            coreStructure.Nodes.Loads = BeamLoadEncoder.ToBeamLoadsArray(
                pointForces, pointMoments, structure.Nodes, structure.ZeroGeometricATol, structure.warnings);

            CoreBeamStructure? coreResult;
            try
            {
                coreResult = MuscleCore.Solvers.BeamDR.Solve(coreStructure, config);
            }
            catch (Exception)
            {
                throw; // rethrow the exception
            }

            if (coreResult == null)
                return null;

            return BeamStructureDecoder.CopyAndUpdate(structure, coreResult);
        }
    }
}
