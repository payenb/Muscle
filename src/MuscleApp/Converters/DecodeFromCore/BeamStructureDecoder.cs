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
using MuscleApp.ViewModel;
using MuscleCore.FEModel;

namespace MuscleApp.Converters
{
    /// <summary>BEAM FE MODEL ONLY - see MuscleApp.ViewModel.BeamStructure / MuscleCore.FEModel.CoreBeamStructure.</summary>
    public static class BeamStructureDecoder
    {
        public static BeamStructure CopyAndUpdate(BeamStructure original, CoreBeamStructure coreResults)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            if (coreResults == null)
                throw new ArgumentNullException(nameof(coreResults));
            if (coreResults.Nodes == null || coreResults.Elements == null)
                throw new ArgumentException("CoreBeamStructure must have valid Nodes and Elements", nameof(coreResults));

            BeamStructure updated = original.Copy();

            updated.Nodes = BeamNodesDecoder.CopyAndUpdate(original.Nodes, coreResults.Nodes);
            updated.Elements = BeamElementsDecoder.CopyAndUpdate(original.Elements, coreResults.Elements, updated.Nodes);
            updated.IsInEquilibrium = coreResults.IsInEquilibrium;

            return updated;
        }
    }
}
