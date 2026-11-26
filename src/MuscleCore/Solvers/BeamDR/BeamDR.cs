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
using Python.Runtime;

namespace MuscleCore.Solvers
{
    /// <summary>
    /// BEAM FE MODEL ONLY - a new, parallel solver, entirely independent of
    /// DynamicRelaxation/StiffnessReductionDynamicRelaxation AND of BeamFEM (Newton-Raphson).
    /// Explicit kinetic-damping Dynamic Relaxation for the Beam FE model: 6 DOF/node. See
    /// musclepy.solvers.beam_dr.main for the full formulation.
    /// </summary>
    public static class BeamDR
    {
        /// <summary>
        /// Solve the Beam FE model to equilibrium via explicit kinetic-damping DR.
        /// </summary>
        /// <param name="structureInitial">Initial structure state (nodes + elements)</param>
        /// <param name="config">Solver configuration - mutated in place with n_time_step/n_ke_reset/converged/residual</param>
        /// <returns>New CoreBeamStructure with the solved state - same result shape as MuscleCore.Solvers.BeamFEM</returns>
        public static CoreBeamStructure? Solve(CoreBeamStructure structureInitial, CoreConfigBeamDR config)
        {
            CoreBeamStructure? result = null;
            try
            {
                var m_threadState = PythonEngine.BeginAllowThreads();
                // Always restore the thread state, also when Python raises: otherwise the next Python
                // call runs without a thread state and aborts the whole process (Rhino included).
                try
                {
                    using (Py.GIL())
                    {
                        PyObject pyElementsObj = structureInitial.Elements.ToPython();
                        dynamic pyElements = pyElementsObj;
                        dynamic pyNodes = pyElements.nodes;

                        PyObject pyConfigObj = config.ToPython();
                        dynamic pyConfig = pyConfigObj;

                        dynamic musclepy = Py.Import("musclepy");
                        musclepy.main_beam_dr(nodes: pyNodes, elements: pyElements, config: pyConfig);

                        CoreBeamElements coreElements = pyElementsObj.As<CoreBeamElements>();
                        CoreBeamNodes coreNodes = coreElements.Nodes;

                        result = new CoreBeamStructure(coreNodes, coreElements, isInEquilibrium: (bool)pyConfig.converged);

                        config.Converged = (bool)pyConfig.converged;
                        config.NTimeStep = (int)pyConfig.n_time_step;
                        config.NKEReset = (int)pyConfig.n_ke_reset;
                        config.Residual = (double)pyConfig.residual;
                    }
                }
                finally
                {
                    PythonEngine.EndAllowThreads(m_threadState);
                }
            }
            catch (Exception)
            {
                throw; // rethrow the exception
            }
            return result;
        }
    }
}
