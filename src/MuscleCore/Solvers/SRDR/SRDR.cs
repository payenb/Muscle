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

using MuscleCore.FEModel;
using Python.Runtime;

namespace MuscleCore.Solvers
{
    /// <summary>
    /// Stiffness-Reduction Dynamic Relaxation (SRDR): accounts for the loss of axial stiffness
    /// of struts due to their initial geometric imperfection (out-of-straightness), following
    /// Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric Imperfections on
    /// the Internal Forces Distribution in Tensegrity Structures", J. Struct. Eng., 2026,
    /// 152(5): 04026039. DOI: 10.1061/JSENDH.STENG-15452.
    /// </summary>
    public static class StiffnessReductionDynamicRelaxation
    {
        /// <summary>
        /// Solve the Stiffness-Reduction Dynamic Relaxation method for a structure with
        /// incremental loads.
        /// </summary>
        /// <param name="coreInitial">Current structure state</param>
        /// <param name="loadsIncrement">[N] - shape (3*nodes.count,) - External load increments to apply</param>
        /// <param name="freeLengthVariation">[m] - shape (elements.count,) - free length variation to apply</param>
        /// <param name="drConfig">Configuration used as a template for every inner Dynamic Relaxation run</param>
        /// <param name="srdrConfig">Configuration for the outer Stiffness-Reduction loop</param>
        /// <param name="inertia">[m^4] - shape (elements.count,) - Second moment of area of elements (struts)</param>
        /// <param name="imperfection">[m] - shape (elements.count,) - Initial imperfection amplitude e0 of elements
        /// (struts). Pass null to let musclepy default to free_length/300 (Eurocode 9) for every element.</param>
        /// <returns>Updated CoreTruss with incremented state</returns>
        public static CoreTruss? Solve(CoreTruss coreInitial, double[] loadsIncrement, double[] freeLengthVariation,
            CoreConfigDR drConfig, CoreConfigSRDR srdrConfig, double[] inertia, double[]? imperfection)
        {
            CoreTruss? coreResult = null;
            try
            {
                var m_threadState = PythonEngine.BeginAllowThreads();
                using (Py.GIL())
                {
                    PyObject pyInitial = coreInitial.ToPython();
                    PyObject pyDrConfig = drConfig.ToPython();
                    PyObject pySrdrConfig = srdrConfig.ToPython();

                    dynamic musclepy = Py.Import("musclepy");
                    dynamic solve = musclepy.main_stiffness_reduction_dr;
                    dynamic pyResult = solve(
                        structure: pyInitial,
                        loads_increment: loadsIncrement,
                        free_length_variation: freeLengthVariation,
                        dr_config: pyDrConfig,
                        srdr_config: pySrdrConfig,
                        inertia: inertia,
                        imperfection: imperfection
                    );

                    coreResult = pyResult.As<CoreTruss>();

                    // Check if structure is in equilibrium (same tolerances the inner DR runs used)
                    dynamic dynamicPyDrConfig = pyDrConfig.As<dynamic>();
                    dynamic rtol = dynamicPyDrConfig.zero_residual_rtol;
                    dynamic atol = dynamicPyDrConfig.zero_residual_atol;
                    coreResult.IsInEquilibrium = pyResult.is_in_equilibrium(rtol, atol).As<bool>();

                    // Update configuration with values from the Python config.
                    // The Python function updates the srdr_config object in-place. Note: dr_config
                    // is NOT mutated here (a fresh copy is used internally for each inner DR run),
                    // so its n_time_step/n_ke_reset counters are not meaningful outputs for SRDR.
                    dynamic dynamicPySrdrConfig = pySrdrConfig.As<dynamic>();
                    srdrConfig.NOuterIterations = dynamicPySrdrConfig.n_outer_iterations.As<int>();
                    srdrConfig.Converged = dynamicPySrdrConfig.converged.As<bool>();
                }
                PythonEngine.EndAllowThreads(m_threadState);
            }
            catch (Exception)
            {
                throw; // rethrow the exception
            }
            return coreResult;
        }
    }
}
