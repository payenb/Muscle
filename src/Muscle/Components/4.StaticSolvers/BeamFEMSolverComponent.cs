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
    /// BEAM FE MODEL ONLY - a new, parallel solver alongside "Dynamic Relaxation Method" and
    /// "Stiffness-Reduction Dynamic Relaxation", entirely independent of them. Corotational
    /// Newton-Raphson beam-truss FE solver: 6 DOF/node, real bending/shear/torsion. "Point
    /// Moments" reuses the same Point Load type as "Point Forces" - same Point3d+Vector3d
    /// shape, but the vector is a moment [N.m], not a force [N].
    /// </summary>
    public class BeamFEMSolverComponent : GH_Component
    {
        private static CoreConfigBeam defaultConfig = new CoreConfigBeam();

        public BeamFEMSolverComponent()
          : base("Solve Beam FEM", "BeamFEM",
              "BEAM FE MODEL ONLY. Solve a Beam Structure (real bending/shear/torsion, per-end hinge releases) via corotational Newton-Raphson FE - unlike Dynamic Relaxation/SRDR (axial only).",
              GHAssemblyName, Folder4_StaticSolvers)
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9f3d7c1a-6e4b-4a8d-b2f5-3c7a1e9d6b8f"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Muscle.View.IconLoader.Get("BeamFEM"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Structure", "BStruct", "A beam structure on which the point forces and point moments will be applied.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Point Forces", "F (kN)", "External point forces to apply on the beam structure (Point Load components, in kN).", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Point Moments", "M (kN.m)", "BEAM-ONLY. External point moments to apply on the beam structure - reuses the same Point Load type as Point Forces, but the vector represents a moment: a Point Load vector entered in kN is read as a moment in kN.m.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Force Tolerance", "tol (N)", "Residual norm below which equilibrium is considered reached.", GH_ParamAccess.item, defaultConfig.ToleranceForce);
            pManager.AddIntegerParameter("Max Iteration", "max it (-)", "Maximum Newton-Raphson iterations per load step.", GH_ParamAccess.item, defaultConfig.MaxIteration);
            pManager.AddIntegerParameter("Load Steps", "steps (-)", "Number of load steps (linear ramp from 0 to full load) - increase for strongly nonlinear cases.", GH_ParamAccess.item, defaultConfig.NbStep);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Structure", "BStruct", "A beam structure containing the total results.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Converged", "Cvg", "True if the Newton-Raphson solver converged within Max Iteration.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Iterations", "it (-)", "The number of Newton-Raphson iterations performed on the last load step.", GH_ParamAccess.item);
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
            double tolForce = -1;
            int maxIteration = -1;
            int nbStep = -1;

            if (!DA.GetData(0, ref gh_structure)) { return; }
            if (!DA.GetDataTree(1, out gh_forces)) { }
            if (!DA.GetDataTree(2, out gh_moments)) { }
            if (!DA.GetData(3, ref tolForce)) { }
            if (!DA.GetData(4, ref maxIteration)) { }
            if (!DA.GetData(5, ref nbStep)) { }

            BeamStructure structure = gh_structure.Value;
            List<PointLoad> forces = GH_Decoders.ToPointLoadList(gh_forces);
            List<PointLoad> moments = GH_Decoders.ToPointLoadList(gh_moments);
            CoreConfigBeam config = new CoreConfigBeam(tolForce, maxIteration, nbStep);

            if (forces.Count == 0 && moments.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No point forces or point moments provided. The structure will not change.");
                DA.SetData(0, gh_structure);
                return;
            }

            BeamStructure result = null;
            try
            {
                result = MuscleApp.Solvers.BeamFEM.Solve(structure, forces, moments, config);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to solve the Beam FEM: {e.Message}");
                return;
            }

            if (result == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to solve the Beam FEM.");
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
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Beam FEM did not converge within {config.MaxIteration} iterations (residual={config.Residual:e2}).");
            }

            DA.SetData(0, new GH_BeamStructure(result));
            DA.SetData(1, config.Converged);
            DA.SetData(2, config.NIteration);
            DA.SetData(3, config.Residual);
        }
    }
}
