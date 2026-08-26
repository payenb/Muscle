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

using Rhino.Geometry;
using System;
using System.Collections.Generic;
using MuscleApp.ViewModel;
using MuscleCore.FEModel;

namespace MuscleApp.Converters
{
    /// <summary>BEAM FE MODEL ONLY - see MuscleApp.ViewModel.BeamNode / MuscleCore.FEModel.CoreBeamNodes.</summary>
    public static class BeamNodesDecoder
    {
        public static List<BeamNode> CopyAndUpdate(List<BeamNode> originalNodes, CoreBeamNodes coreNodesResults)
        {
            if (originalNodes == null)
                throw new ArgumentNullException(nameof(originalNodes));
            if (coreNodesResults == null)
                throw new ArgumentNullException(nameof(coreNodesResults));
            if (originalNodes.Count != coreNodesResults.Count)
                throw new ArgumentException("Nodes list and CoreBeamNodes arrays must have the same length");

            List<BeamNode> updatedNodes = new List<BeamNode>();

            for (int i = 0; i < coreNodesResults.Count; i++)
            {
                BeamNode updatedNode = originalNodes[i].Copy();

                updatedNode.Coordinates = new Point3d(
                    coreNodesResults.Coordinates[i, 0],
                    coreNodesResults.Coordinates[i, 1],
                    coreNodesResults.Coordinates[i, 2]
                );

                updatedNode.Loads = new Vector3d(coreNodesResults.Loads[i, 0], coreNodesResults.Loads[i, 1], coreNodesResults.Loads[i, 2]);
                updatedNode.MomentLoads = new Vector3d(coreNodesResults.Loads[i, 3], coreNodesResults.Loads[i, 4], coreNodesResults.Loads[i, 5]);

                updatedNode.Reactions = new Vector3d(coreNodesResults.Reactions[i, 0], coreNodesResults.Reactions[i, 1], coreNodesResults.Reactions[i, 2]);
                updatedNode.MomentReactions = new Vector3d(coreNodesResults.Reactions[i, 3], coreNodesResults.Reactions[i, 4], coreNodesResults.Reactions[i, 5]);

                updatedNode.Residuals = new Vector3d(coreNodesResults.Residuals[i, 0], coreNodesResults.Residuals[i, 1], coreNodesResults.Residuals[i, 2]);
                updatedNode.MomentResiduals = new Vector3d(coreNodesResults.Residuals[i, 3], coreNodesResults.Residuals[i, 4], coreNodesResults.Residuals[i, 5]);

                updatedNodes.Add(updatedNode);
            }

            return updatedNodes;
        }
    }
}
