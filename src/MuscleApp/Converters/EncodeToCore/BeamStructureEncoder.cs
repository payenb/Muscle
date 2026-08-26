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

using MuscleCore.FEModel;
using MuscleApp.ViewModel;
using System;
using static MuscleApp.Converters.BeamNodesEncoder;
using static MuscleApp.Converters.BeamElementsEncoder;

namespace MuscleApp.Converters
{
    /// <summary>BEAM FE MODEL ONLY - see MuscleApp.ViewModel.BeamStructure / MuscleCore.FEModel.CoreBeamStructure.</summary>
    public static class BeamStructureEncoder
    {
        public static CoreBeamStructure ToCore(BeamStructure structure)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
            if (structure.Nodes.Count == 0)
                throw new ArgumentException("BeamStructure must have at least one node", nameof(structure));
            if (structure.Elements.Count == 0)
                throw new ArgumentException("BeamStructure must have at least one element", nameof(structure));

            CoreBeamNodes coreNodes = ToCoreBeamNodes(structure.Nodes);
            CoreBeamElements coreElements = ToCoreBeamElements(structure.Elements, coreNodes);

            return new CoreBeamStructure(coreNodes, coreElements);
        }
    }
}
