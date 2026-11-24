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

using System.Collections.Generic;
using System.Linq;
using MuscleApp.ViewModel;
using MuscleApp.Converters;
using MuscleCore.FEModel;
using MuscleCore.Solvers;

namespace MuscleApp.Solvers
{
    public static class StiffnessReductionDynamicRelaxation
    {
        /// <summary>
        /// Solve the Stiffness-Reduction Dynamic Relaxation method for a structure with
        /// incremental loads and prestress (free length changes), accounting for the loss of
        /// axial stiffness of struts due to their initial geometric imperfection.
        /// </summary>
        /// <param name="truss">Current structure state</param>
        /// <param name="pointLoads">Collection of PointLoad instances defining external loads to apply</param>
        /// <param name="prestress">Collection of Prestress instances defining free length variations to apply</param>
        /// <param name="drConfig">Configuration used as a template for every inner Dynamic Relaxation run</param>
        /// <param name="srdrConfig">Configuration for the outer Stiffness-Reduction loop</param>
        /// <param name="imperfectionRatio">[-] e0/L0 for each element, in truss.Elements order. Pass null to let
        /// musclepy default to 1/300 (Eurocode 9) for every element.</param>
        /// <returns>Updated Truss with incremented state</returns>
        public static Truss? Solve(Truss truss, IEnumerable<PointLoad> pointLoads, IEnumerable<Prestress> prestress,
            CoreConfigDR drConfig, CoreConfigSRDR srdrConfig, IEnumerable<double>? imperfectionRatio = null)
        {
            // 1) Convert the input data to the core data types
            // Create a free length variation array by adding up all prestress for each element.
            double[] freeLengthVariation = PrestressEncoder.AddsUpAllPrestress(prestress, truss.Elements.Count);

            // Create a load increment array by adding up all point loads for each node.
            double[] loadsIncrement = PointLoadEncoder.AddsUpAllPointLoads(pointLoads, truss);

            // [m^4] Second moment of area of each element's cross section, needed to compute the Euler
            // critical load and slenderness ratio of struts (Element.CS.Inertia is already computed
            // from the cross section's dimensions, see MuscleApp.ViewModel.CrossSection).
            // Passed in SI like every other quantity: musclepy never converts units.
            double[] inertia = truss.Elements.Select(e => e.CS.Inertia).ToArray();

            // Initial imperfection amplitude e0, as a ratio of each element's free length.
            // If not provided, pass null through so musclepy applies its own default (L0/300).
            double[]? imperfection = null;
            if (imperfectionRatio != null)
            {
                var ratios = imperfectionRatio.ToArray();
                imperfection = truss.Elements
                    .Select((e, i) => (i < ratios.Length ? ratios[i] : ratios.LastOrDefault()) * e.FreeLength)
                    .ToArray();
            }

            // 2) Call the core solver
            CoreTruss? coreResult = null;
            try
            {
                coreResult = MuscleCore.Solvers.StiffnessReductionDynamicRelaxation.Solve(
                    TrussEncoder.ToCore(truss), loadsIncrement, freeLengthVariation,
                    drConfig, srdrConfig, inertia, imperfection);
            }
            catch (Exception)
            {
                throw; // rethrow the exception
            }

            // 3) Update the input structure with the results
            if (coreResult == null)
                return null;

            var updatedTruss = TrussDecoder.CopyAndUpdate(truss, coreResult);

            return updatedTruss;
        }
    }
}
