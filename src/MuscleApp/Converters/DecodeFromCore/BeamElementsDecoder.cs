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
    /// <summary>BEAM FE MODEL ONLY - see MuscleApp.ViewModel.BeamElement / MuscleCore.FEModel.CoreBeamElements.</summary>
    public static class BeamElementsDecoder
    {
        public static List<BeamElement> CopyAndUpdate(List<BeamElement> originalElements, CoreBeamElements coreElementsResults, List<BeamNode> updatedNodes)
        {
            if (originalElements == null)
                throw new ArgumentNullException(nameof(originalElements));
            if (coreElementsResults == null)
                throw new ArgumentNullException(nameof(coreElementsResults));
            if (updatedNodes == null || updatedNodes.Count == 0)
                throw new ArgumentException("Nodes collection cannot be null or empty", nameof(updatedNodes));
            if (originalElements.Count != coreElementsResults.Count)
                throw new ArgumentException("Elements list and CoreBeamElements arrays must have the same length");

            List<BeamElement> updatedElements = new List<BeamElement>();

            for (int i = 0; i < coreElementsResults.Count; i++)
            {
                BeamElement updatedElement = originalElements[i].Copy();

                int node_idx0 = updatedElement.EndNodes[0];
                int node_idx1 = updatedElement.EndNodes[1];
                Point3d p0 = updatedNodes[node_idx0].Coordinates;
                Point3d p1 = updatedNodes[node_idx1].Coordinates;
                updatedElement.Line = new Line(p0, p1);

                if (coreElementsResults.FreeLength != null)
                    updatedElement.FreeLength = coreElementsResults.FreeLength[i];

                if (coreElementsResults.Tension != null)
                    updatedElement.Tension = coreElementsResults.Tension[i];

                if (coreElementsResults.MomentStart != null)
                {
                    updatedElement.MyStart = coreElementsResults.MomentStart[i, 0];
                    updatedElement.MzStart = coreElementsResults.MomentStart[i, 1];
                }

                if (coreElementsResults.MomentEnd != null)
                {
                    updatedElement.MyEnd = coreElementsResults.MomentEnd[i, 0];
                    updatedElement.MzEnd = coreElementsResults.MomentEnd[i, 1];
                }

                if (coreElementsResults.Torsion != null)
                    updatedElement.Torsion = coreElementsResults.Torsion[i];

                updatedElements.Add(updatedElement);
            }

            return updatedElements;
        }
    }
}
