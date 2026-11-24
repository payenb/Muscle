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

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Stiffness-Reduction Dynamic Relaxation (SRDR): accounts for the loss of axial stiffness
    /// of struts due to their initial geometric imperfection (out-of-straightness), following
    /// Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric Imperfections on
    /// the Internal Forces Distribution in Tensegrity Structures", J. Struct. Eng., 2026,
    /// 152(5): 04026039. DOI: 10.1061/JSENDH.STENG-15452.
    /// </summary>
    public class SRDRSolverComponent : GH_Component
    {
        private static CoreConfigDR defaultDrConfig = new CoreConfigDR();
        private static double default_dt = defaultDrConfig.Dt;
        private static double default_rtol = defaultDrConfig.ZeroResidualRTol;
        private static double default_atol = defaultDrConfig.ZeroResidualATol;
        private static int default_maxTimeStep = defaultDrConfig.MaxTimeStep;
        private static int default_maxPeakReset = defaultDrConfig.MaxKEResets;
        private static double default_amplMass = defaultDrConfig.MassAmplFactor;
        private static double default_minMass = defaultDrConfig.MinMass;

        private static CoreConfigSRDR defaultSrdrConfig = new CoreConfigSRDR();
        private static double default_forceTol = defaultSrdrConfig.ForceTol;
        private static int default_maxOuterIterations = defaultSrdrConfig.MaxOuterIterations;
        private static bool default_warmStart = defaultSrdrConfig.WarmStart;

        /// <summary>
        /// Initializes a new instance of the SRDRSolverComponent class.
        /// </summary>
        public SRDRSolverComponent()
          : base("Stiffness-Reduction Dynamic Relaxation", "SRDR",
                "Solve form-finding, deployment, nonlinear loading, and nonlinear prestressing problems, " +
                "accounting for the loss of axial stiffness of struts due to their initial geometric imperfection (out-of-straightness)",
              GHAssemblyName, Folder4_StaticSolvers)
        {
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Muscle.View.IconLoader.Get("SRDR");
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9454c1ef-3397-486c-88ef-4274c2c88f64"); }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "struct", "A structure on which the point loads and prestress will be applied via the stiffness-reduction dynamic relaxation method.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Point Loads", "Load (kN)", "The external point loads to apply on the structure.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Prestress", "P (m)", "Prestress Scenario containing the free length variations (m) to apply on the specified elements.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Imperfection Ratio", "e0/L0 (-)", "Initial imperfection amplitude of each strut, as a ratio of its free length. One value per element (in the Structure's element order), or a single value applied to every element. If not provided, defaults to 1/300 (Eurocode 9) for every element.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Relative tolerance", "rtol (-)", "Set the relative tolerance for equilibrium of each inner Dynamic Relaxation run. Equilibrium is achieved if each residual load is equal to 0 [N] (within tolerance : rtol * load + atol).", GH_ParamAccess.item, default_rtol);
            pManager.AddNumberParameter("Absolute tolerance", "atol (N)", "Set the absolute tolerance for equilibrium of each inner Dynamic Relaxation run. Equilibrium is achieved if each residual load is equal to 0 [N] (within tolerance : rtol * load + atol).", GH_ParamAccess.item, default_atol);
            pManager.AddNumberParameter("Delta t", "Dt (s)", "The time increment for each inner Dynamic Relaxation run.", GH_ParamAccess.item, default_dt);
            pManager.AddIntegerParameter("Max Time Step", "max it (-)", "Maximum number of time increments before an inner Dynamic Relaxation run aborts looking for the equilibrium.", GH_ParamAccess.item, default_maxTimeStep);
            pManager.AddIntegerParameter("Max Peak Reset", "max peak (/)", "Maximum number of kinetic energy resets before an inner Dynamic Relaxation run aborts looking for the equilibrium.", GH_ParamAccess.item, default_maxPeakReset);
            pManager.AddNumberParameter("Mass Amplification", "Ampl (/)", "Amplification factor for the fictitious masses in case of convergence issue.", GH_ParamAccess.item, default_amplMass);
            pManager.AddNumberParameter("Minimum Mass", "Min (kg)", "Minimum fictitious mass applied on the degrees of freedom where fictitious mass equals 0 (i.e. 0 tangent stiffness).", GH_ParamAccess.item, default_minMass);
            pManager.AddNumberParameter("Force Tolerance", "Ftol (N)", "Convergence tolerance on the maximum strut axial force change between two successive outer (stiffness-reduction) iterations.", GH_ParamAccess.item, default_forceTol);
            pManager.AddIntegerParameter("Max Outer Iterations", "max outer (-)", "Maximum number of outer (stiffness-reduction) iterations before the solver aborts looking for a converged stiffness.", GH_ParamAccess.item, default_maxOuterIterations);
            pManager.AddBooleanParameter("Warm Start", "warm (-)", "If true, each outer iteration's Dynamic Relaxation run starts from the previous one's converged geometry (faster). If false, it always restarts from the original input structure.", GH_ParamAccess.item, default_warmStart);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;
            pManager[12].Optional = true;
            pManager[13].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "struct", "A structure containing the total results.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsInEquilibrium", "Equilibrium", "True if the external load are in equilibrium with the internal forces.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Outer Iterations", "outer it (/)", "The number of outer (stiffness-reduction) iterations required to reach a converged stiffness.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Converged", "converged", "True if the outer stiffness-reduction loop converged within Max Outer Iterations.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Check if Python.NET is initialized
            if (!PythonNETManager.IsInitialized)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Python has not been started. Please start the 'StartPython.NET' component first.");
                return;
            }

            //1) Collect Data
            GH_Truss gh_truss = new GH_Truss();
            GH_Structure<IGH_Goo> gh_loads = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> gh_prestress = new GH_Structure<IGH_Goo>();
            List<double> imperfectionRatio = new List<double>();
            double rtol = -1;
            double atol = -1;
            double dt = -1;
            int maxTimeStep = -1;
            int maxKEReset = -1;
            double amplMass = -1;
            double minMass = -1;
            double forceTol = -1;
            int maxOuterIterations = -1;
            bool warmStart = default_warmStart;

            if (!DA.GetData(0, ref gh_truss)) { return; }
            if (!DA.GetDataTree(1, out gh_loads)) { }
            if (!DA.GetDataTree(2, out gh_prestress)) { }
            DA.GetDataList(3, imperfectionRatio);
            if (!DA.GetData(4, ref rtol)) { }
            if (!DA.GetData(5, ref atol)) { }
            if (!DA.GetData(6, ref dt)) { }
            if (!DA.GetData(7, ref maxTimeStep)) { }
            if (!DA.GetData(8, ref maxKEReset)) { }
            if (!DA.GetData(9, ref amplMass)) { }
            if (!DA.GetData(10, ref minMass)) { }
            if (!DA.GetData(11, ref forceTol)) { }
            if (!DA.GetData(12, ref maxOuterIterations)) { }
            if (!DA.GetData(13, ref warmStart)) { }

            // 2) Transform data before solving
            Truss truss = gh_truss.Value;
            List<PointLoad> pointLoads = GH_Decoders.ToPointLoadList(gh_loads);
            List<Prestress> prestress = GH_Decoders.ToPrestressList(gh_prestress);
            CoreConfigDR drConfig = new CoreConfigDR
            {
                Dt = dt,
                MassAmplFactor = amplMass,
                MinMass = minMass,
                MaxTimeStep = maxTimeStep,
                MaxKEResets = maxKEReset,
                ZeroResidualRTol = rtol,
                ZeroResidualATol = atol
            };
            CoreConfigSRDR srdrConfig = new CoreConfigSRDR
            {
                ForceTol = forceTol,
                MaxOuterIterations = maxOuterIterations,
                WarmStart = warmStart
            };

            // Check if we have any loads or prestress to apply
            if (pointLoads.Count == 0 && prestress.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No loads or prestress provided. The structure will not change.");
                DA.SetData(0, gh_truss);
                return;
            }

            Truss result = null;
            // 3) Solve using the SRDR solver
            try
            {
                result = MuscleApp.Solvers.StiffnessReductionDynamicRelaxation.Solve(
                    truss, pointLoads, prestress, drConfig, srdrConfig,
                    imperfectionRatio.Count > 0 ? imperfectionRatio : null);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to solve the stiffness-reduction dynamic relaxation method: {e.Message}");
                return;
            }

            // 4) Check if the solution was successful
            if (result == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to solve the stiffness-reduction dynamic relaxation method.");
                return;
            }

            // 5) Check for warnings from the solver
            if (truss.warnings != null && truss.warnings.Count > 0)
            {
                foreach (string warning in truss.warnings)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);
                }
                truss.warnings.Clear();
            }

            if (!srdrConfig.Converged)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"The outer stiffness-reduction loop did not converge within {srdrConfig.MaxOuterIterations} iterations.");
            }

            // 6) Set output
            GH_Truss gh_result = new GH_Truss(result);
            DA.SetData(0, gh_result);
            DA.SetData(1, result.IsInEquilibrium);
            DA.SetData(2, srdrConfig.NOuterIterations);
            DA.SetData(3, srdrConfig.Converged);

        }
    }
}
