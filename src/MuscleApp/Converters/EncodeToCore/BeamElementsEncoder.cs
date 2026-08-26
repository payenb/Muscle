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
    /// <summary>BEAM FE MODEL ONLY - see MuscleApp.ViewModel.BeamElement / MuscleCore.FEModel.CoreBeamElements.</summary>
    public static class BeamElementsEncoder
    {
        public static CoreBeamElements ToCoreBeamElements(IEnumerable<BeamElement> elements, CoreBeamNodes coreNodes)
        {
            if (elements == null || !elements.Any())
                throw new ArgumentException("Elements collection cannot be null or empty");
            if (coreNodes == null)
                throw new ArgumentNullException(nameof(coreNodes));

            int count = elements.Count();

            int[,] endNodes = new int[count, 2];
            double[] area = new double[count];
            double[] youngs = new double[count];
            double[] inertia = new double[count];
            double[] torsionConstant = new double[count];
            double[] shearModulus = new double[count];
            double[] freeLength = new double[count];
            bool[] hingeStart = new bool[count];
            bool[] hingeEnd = new bool[count];
            double[,] orientation = new double[count, 3];

            int i = 0;
            foreach (var element in elements)
            {
                if (element.EndNodes.Count < 2)
                    throw new ArgumentException($"Beam element at index {i} does not have exactly 2 end nodes");

                endNodes[i, 0] = element.EndNodes[0];
                endNodes[i, 1] = element.EndNodes[1];

                area[i] = element.CS.Area;
                youngs[i] = element.Material.Ec; // single Young's modulus - see BeamElement docstring
                inertia[i] = element.CS.Inertia;
                torsionConstant[i] = element.CS.J;
                shearModulus[i] = element.Material.G;
                freeLength[i] = element.FreeLength;

                hingeStart[i] = element.HingeStart;
                hingeEnd[i] = element.HingeEnd;

                if (element.Orientation.HasValue)
                {
                    orientation[i, 0] = element.Orientation.Value.X;
                    orientation[i, 1] = element.Orientation.Value.Y;
                    orientation[i, 2] = element.Orientation.Value.Z;
                }
                else
                {
                    // NaN row = "automatic" orientation - see PyBeamElements.orientation
                    orientation[i, 0] = double.NaN;
                    orientation[i, 1] = double.NaN;
                    orientation[i, 2] = double.NaN;
                }

                i++;
            }

            return new CoreBeamElements(
                coreNodes,
                endNodes: endNodes,
                area: area,
                youngs: youngs,
                inertia: inertia,
                torsionConstant: torsionConstant,
                shearModulus: shearModulus,
                freeLength: freeLength,
                hingeStart: hingeStart,
                hingeEnd: hingeEnd,
                orientation: orientation
            );
        }
    }
}
