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

using MuscleApp.ViewModel;
using System;
using System.Collections.Generic;

namespace MuscleApp.Converters
{
    /// <summary>
    /// BEAM FE MODEL ONLY. Rather than inventing a new "BeamPointLoad" GH type, the Beam FE
    /// model reuses the existing PointLoad/GH_PointLoad/"Construct Point Load" component for
    /// BOTH force and moment inputs - same Point3d+Vector3d shape, just accumulated into
    /// different columns of the (n,6) loads array (force: columns 0-2, moment: columns 3-5).
    /// </summary>
    public static class BeamLoadEncoder
    {
        /// <summary>
        /// Combines separate force and moment PointLoad collections into a single (n,6)
        /// loads array [Fx,Fy,Fz,Mx,My,Mz] matching CoreBeamNodes.Loads' layout.
        /// </summary>
        public static double[,] ToBeamLoadsArray(IEnumerable<PointLoad> forces, IEnumerable<PointLoad> moments,
            List<BeamNode> nodes, double zeroTolerance, List<string>? warnings = null)
        {
            if (nodes == null || nodes.Count == 0)
                throw new ArgumentException("Nodes list cannot be null or empty", nameof(nodes));

            double[,] loads = new double[nodes.Count, 6];
            AccumulateInto(loads, forces, nodes, zeroTolerance, columnOffset: 0, "force", warnings);
            AccumulateInto(loads, moments, nodes, zeroTolerance, columnOffset: 3, "moment", warnings);
            return loads;
        }

        private static void AccumulateInto(double[,] loads, IEnumerable<PointLoad> pointLoads, List<BeamNode> nodes,
            double zeroTolerance, int columnOffset, string kind, List<string>? warnings)
        {
            if (pointLoads == null) return;

            foreach (PointLoad load in pointLoads)
            {
                if (load == null || !load.IsValid) continue;

                int nodeIdx = -1;
                if (load.NodeIdx >= 0)
                {
                    nodeIdx = load.NodeIdx;
                    if (nodeIdx >= nodes.Count)
                    {
                        warnings?.Add($"A point {kind} is applied on node index {nodeIdx} which does not exist in the beam structure. This {kind} is ignored.");
                        continue;
                    }
                }
                else if (!BeamNode.EpsilonContains(nodes, load.Point, zeroTolerance, out nodeIdx))
                {
                    warnings?.Add($"A point {kind} is applied on a point ({load.Point.X:F3}, {load.Point.Y:F3}, {load.Point.Z:F3}) which does not belong to the beam structure. This {kind} is ignored.");
                    continue;
                }

                loads[nodeIdx, columnOffset] += load.Vector.X;
                loads[nodeIdx, columnOffset + 1] += load.Vector.Y;
                loads[nodeIdx, columnOffset + 2] += load.Vector.Z;
            }
        }
    }
}
