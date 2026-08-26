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
using System.Collections.Generic;
using System.Linq;

namespace MuscleApp.Converters
{
    /// <summary>BEAM FE MODEL ONLY - see MuscleApp.ViewModel.BeamNode / MuscleCore.FEModel.CoreBeamNodes.</summary>
    public static class BeamNodesEncoder
    {
        public static CoreBeamNodes ToCoreBeamNodes(IEnumerable<BeamNode> nodes)
        {
            if (nodes == null || !nodes.Any())
                throw new ArgumentException("Nodes collection cannot be null or empty");

            int count = nodes.Count();

            double[,] initialCoordinates = new double[count, 3];
            bool[,] dof = new bool[count, 6];
            double[,] loads = new double[count, 6];

            int i = 0;
            foreach (BeamNode node in nodes)
            {
                initialCoordinates[i, 0] = node.Coordinates.X;
                initialCoordinates[i, 1] = node.Coordinates.Y;
                initialCoordinates[i, 2] = node.Coordinates.Z;

                dof[i, 0] = node.isXFree;
                dof[i, 1] = node.isYFree;
                dof[i, 2] = node.isZFree;
                dof[i, 3] = node.isRxFree;
                dof[i, 4] = node.isRyFree;
                dof[i, 5] = node.isRzFree;

                loads[i, 0] = node.Loads.X;
                loads[i, 1] = node.Loads.Y;
                loads[i, 2] = node.Loads.Z;
                loads[i, 3] = node.MomentLoads.X;
                loads[i, 4] = node.MomentLoads.Y;
                loads[i, 5] = node.MomentLoads.Z;

                i++;
            }

            return new CoreBeamNodes(initialCoordinates, dof, loads);
        }
    }
}
