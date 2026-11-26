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
// ------------------------------------------------------------------------------------------------------------

using MuscleApp.ViewModel;
using MuscleApp.Converters;
using MuscleCore.Solvers;

namespace MuscleApp.Solvers
{
    public static class DynamicModalAnalysis
    {
        /// <summary>
        /// Dynamic modal analysis: natural frequencies and modes of the structure in its current state.
        /// </summary>
        /// <param name="truss">Current structure state (e.g. prestressed and under its permanent loads, from a solver)</param>
        /// <param name="pointMasses">Collection of PointMass instances defining point masses to apply on the nodes</param>
        /// <param name="elementMasses">[kg] mass of each element, in the order of truss.Elements. If empty, the masses
        /// are computed from the elements: free length x area x specific mass of the material (Element.Mass).</param>
        /// <param name="elementMassesOption">0: neglect element masses, 1: lumped, 2: consistent mass matrix</param>
        /// <param name="nModes">Number of modes to compute (0: all)</param>
        /// <returns>Natural frequencies, modes and masses</returns>
        public static ResultsDynamic? Solve(Truss truss, IEnumerable<PointMass> pointMasses, IEnumerable<double> elementMasses, int elementMassesOption, int nModes)
        {
            // 1) Convert the input data to the core data types
            // Create a pointmasses array by adding up all point loads for each node.
            double[] pointMassArray = PointMassEncoder.AddsUpAllPointMasses(pointMasses, truss);

            // Element masses: computed from the elements when none are given, as the Grasshopper input says
            double[] elementMassArray = elementMasses.ToArray();
            if (elementMassArray.Length == 0)
            {
                elementMassArray = truss.Elements.Select(e => e.Mass).ToArray();
            }

            // 2) Call the core solver 
            CoreResultsDynamic? coreResults = MuscleCore.Solvers.DynamicModalAnalysis.Solve(TrussEncoder.ToCore(truss), pointMassArray, elementMassArray, elementMassesOption, nModes);
            
            return new ResultsDynamic(coreResults);
        }
    }
}
