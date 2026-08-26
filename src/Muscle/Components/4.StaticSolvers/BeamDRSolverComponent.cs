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

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MuscleCore.Solvers;
using MuscleCore.PythonNETInit;
using MuscleApp.ViewModel;
using MuscleApp.Solvers;
using Muscle.View;
using Muscle.Converters;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.Solvers
{
    /// <summary>
    /// BEAM FE MODEL ONLY - a new, parallel solver alongside "Dynamic Relaxation Method",
    /// "Stiffness-Reduction Dynamic Relaxation" and "Solve Beam FEM", entirely independent of
    /// them. Explicit, mass-based kinetic-damping Dynamic Relaxation for the Beam FE model
    /// (6 DOF/node, real bending/shear/torsion) - unlike Solve Beam FEM (Newton-Raphson,
    /// assembles and solves a global stiffness matrix), this never assembles/solves a linear
    /// system, matching the same explicit, mass/velocity-based approach as "Dynamic
    /// Relaxation Method" itself, generalized to rotational DOF.
    /// </summary>
    public class BeamDRSolverComponent : GH_Component
    {
        private static CoreConfigBeamDR defaultConfig = new CoreConfigBeamDR();

        public BeamDRSolverComponent()
          : base("Solve Beam DR", "BeamDR",
              "BEAM FE MODEL ONLY. Solve a Beam Structure via explicit kinetic-damping Dynamic Relaxation (6 DOF/node, fictitious mass/inertia from the stiffness diagonal, no matrix assembly/solve) - the same method as Dynamic Relaxation Method, generalized to real bending/shear/torsion.",
              GHAssemblyName, Folder4_StaticSolvers)
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("3a7e9c2d-5f1b-4d6a-8e3c-9b2d5a7f1c4e"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return null; }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Structure", "BStruct", "A beam structure on which the point forces and point moments will be applied.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Point Forces", "F (N)", "External point forces to apply on the beam structure.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Point Moments", "M (N.m)", "BEAM-ONLY. External point moments to apply on the beam structure - reuses the same Point Load type as Point Forces, but the vector represents a moment.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Delta t", "Dt (s)", "The time increment for the dynamic relaxation method.", GH_ParamAccess.item, defaultConfig.Dt);
            pManager.AddNumberParameter("Mass Amplification", "Ampl (/)", "Amplification factor for the fictitious masses in case of convergence issue.", GH_ParamAccess.item, defaultConfig.MassAmplFactor);
            pManager.AddIntegerParameter("Max Time Step", "max it (-)", "Maximum number of time increments before the solver aborts looking for the equilibrium.", GH_ParamAccess.item, defaultConfig.MaxTimeStep);
            pManager.AddIntegerParameter("Max Peak Reset", "max peak (/)", "Maximum number of kinetic energy resets before the solver aborts looking for the equilibrium.", GH_ParamAccess.item, defaultConfig.MaxKEResets);
            pManager.AddNumberParameter("Force Tolerance", "tol (N)", "Residual norm below which equilibrium is considered reached.", GH_ParamAccess.item, defaultConfig.ToleranceForce);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Structure", "BStruct", "A beam structure containing the total results.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Converged", "Cvg", "True if the equilibrium was reached within Max Time Step / Max Peak Reset.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Time Step", "it (/)", "The number of time increments required to reach the equilibrium.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Peak Reset", "peaks (/)", "The number of detected kinetic energy peaks.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Residual", "R (N)", "Final residual norm.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!PythonNETManager.IsInitialized)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Python has not been started. Please start the 'StartPython.NET' component first.");
                return;
            }

            GH_BeamStructure gh_structure = new GH_BeamStructure();
            GH_Structure<IGH_Goo> gh_forces = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> gh_moments = new GH_Structure<IGH_Goo>();
            double dt = -1;
            double amplMass = -1;
            int maxTimeStep = -1;
            int maxKEReset = -1;
            double tolForce = -1;

            if (!DA.GetData(0, ref gh_structure)) { return; }
            if (!DA.GetDataTree(1, out gh_forces)) { }
            if (!DA.GetDataTree(2, out gh_moments)) { }
            if (!DA.GetData(3, ref dt)) { }
            if (!DA.GetData(4, ref amplMass)) { }
            if (!DA.GetData(5, ref maxTimeStep)) { }
            if (!DA.GetData(6, ref maxKEReset)) { }
            if (!DA.GetData(7, ref tolForce)) { }

            BeamStructure structure = gh_structure.Value;
            List<PointLoad> forces = GH_Decoders.ToPointLoadList(gh_forces);
            List<PointLoad> moments = GH_Decoders.ToPointLoadList(gh_moments);
            CoreConfigBeamDR config = new CoreConfigBeamDR(dt, amplMass, defaultConfig.MinMass, maxTimeStep, maxKEReset, tolForce);

            if (forces.Count == 0 && moments.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No point forces or point moments provided. The structure will not change.");
                DA.SetData(0, gh_structure);
                return;
            }

            BeamStructure result = null;
            try
            {
                result = MuscleApp.Solvers.BeamDR.Solve(structure, forces, moments, config);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to solve the Beam DR: {e.Message}");
                return;
            }

            if (result == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to solve the Beam DR.");
                return;
            }

            if (structure.warnings != null && structure.warnings.Count > 0)
            {
                foreach (string warning in structure.warnings)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);
                }
                structure.warnings.Clear();
            }

            if (!config.Converged)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Beam DR did not converge within {config.MaxTimeStep} time steps / {config.MaxKEResets} peak resets (residual={config.Residual:e2}).");
            }

            DA.SetData(0, new GH_BeamStructure(result));
            DA.SetData(1, config.Converged);
            DA.SetData(2, config.NTimeStep);
            DA.SetData(3, config.NKEReset);
            DA.SetData(4, config.Residual);
        }
    }
}
