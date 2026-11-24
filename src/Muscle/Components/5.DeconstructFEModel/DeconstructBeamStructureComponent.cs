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
using Grasshopper.Kernel;
using Muscle.View;
using MuscleApp.ViewModel;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.DeconstructFEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - see DeconstructStructureComponent for the axial equivalent.
    /// </summary>
    public class DeconstructBeamStructureComponent : GH_Component
    {
        public DeconstructBeamStructureComponent()
          : base("Deconstruct Beam Structure", "DeBeamStruct",
              "BEAM FE MODEL ONLY. Deconstruct a beam structure into its beam nodes and beam elements.",
              GHAssemblyName, Folder5_DeconstructFEM)
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Structure", "BStruct", "A Beam Structure is made of Beam Nodes and Beam Elements", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Nodes", "BN", "The beam nodes composing the structure.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Beam Elements", "BE", "The beam finite elements composing the structure.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            BeamStructure structure = new BeamStructure();

            if (!DA.GetData(0, ref structure)) { return; }

            DA.SetDataList(0, structure.Nodes);
            DA.SetDataList(1, structure.Elements);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Muscle.View.IconLoader.Get("DeconstructBeamStructure"); }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("2d5f8a3c-7b1e-4d9a-8f2c-6a4b3e7d1c9a"); }
        }
    }
}
