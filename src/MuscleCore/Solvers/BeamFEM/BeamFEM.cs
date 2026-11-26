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
    /// DynamicRelaxation/StiffnessReductionDynamicRelaxation. Corotational Newton-Raphson
    /// beam-truss FE solver: 6 DOF/node, 2-node beam-column elements with per-end hinge
    /// releases. See musclepy.solvers.beam_fem.main for the full formulation, ported from
    /// Simon Lizee's thesis ("Numerical models for tensegrity", UCLouvain, 2026).
    /// </summary>
    public static class BeamFEM
    {
        /// <summary>
        /// Solve the Beam FE model to equilibrium.
        /// </summary>
        /// <param name="structureInitial">Initial structure state (nodes + elements)</param>
        /// <param name="config">Solver configuration - mutated in place with n_iteration/converged/residual</param>
        /// <returns>New CoreBeamStructure with the solved state (displacements, reactions, tension, moments, torsion)</returns>
        public static CoreBeamStructure? Solve(CoreBeamStructure structureInitial, CoreConfigBeam config)
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
                        musclepy.main_beam_fem(nodes: pyNodes, elements: pyElements, config: pyConfig);

                        CoreBeamElements coreElements = pyElementsObj.As<CoreBeamElements>();
                        CoreBeamNodes coreNodes = coreElements.Nodes;

                        result = new CoreBeamStructure(coreNodes, coreElements, isInEquilibrium: (bool)pyConfig.converged);

                        config.Converged = (bool)pyConfig.converged;
                        config.NIteration = (int)pyConfig.n_iteration;
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
