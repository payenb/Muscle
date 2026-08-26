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
using Rhino.Geometry;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.ConstructFEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - constructs a MuscleApp.ViewModel.BeamSupport (6 DOF: 3
    /// translations + 3 rotations), entirely separate from "Construct Support" (3
    /// translations only, used by DR/SRDR).
    /// </summary>
    public class BeamSupportComponent : GH_Component
    {
        public BeamSupportComponent()
          : base("Construct Beam Support", "BeamSpt",
              "BEAM FE MODEL ONLY. A Beam Support fixes some of the 6 DOF (X,Y,Z translations and Rx,Ry,Rz rotations) of a Beam Node - unlike Construct Support (3 translations only, for DR/SRDR).",
              GHAssemblyName, Folder2_ConstructFEM)
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7c2e9a4d-5b8f-4e1a-8c3d-2f6a9b1e4d7c"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return null; }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Pt", "Point of application of the beam support", GH_ParamAccess.item); //0
            pManager.AddBooleanParameter("IsXFree", "X", "Is the X translation free?", GH_ParamAccess.item, false); //1
            pManager.AddBooleanParameter("IsYFree", "Y", "Is the Y translation free?", GH_ParamAccess.item, false); //2
            pManager.AddBooleanParameter("IsZFree", "Z", "Is the Z translation free?", GH_ParamAccess.item, false); //3
            pManager.AddBooleanParameter("IsRxFree", "Rx", "BEAM-ONLY DOF. Is the rotation about X free?", GH_ParamAccess.item, false); //4
            pManager.AddBooleanParameter("IsRyFree", "Ry", "BEAM-ONLY DOF. Is the rotation about Y free?", GH_ParamAccess.item, false); //5
            pManager.AddBooleanParameter("IsRzFree", "Rz", "BEAM-ONLY DOF. Is the rotation about Z free?", GH_ParamAccess.item, false); //6

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Support", "BSpt", "Beam Support", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d point = new Point3d();
            bool isXFree = false, isYFree = false, isZFree = false;
            bool isRxFree = false, isRyFree = false, isRzFree = false;

            if (!DA.GetData(0, ref point)) { return; }
            if (!DA.GetData(1, ref isXFree)) { }
            if (!DA.GetData(2, ref isYFree)) { }
            if (!DA.GetData(3, ref isZFree)) { }
            if (!DA.GetData(4, ref isRxFree)) { }
            if (!DA.GetData(5, ref isRyFree)) { }
            if (!DA.GetData(6, ref isRzFree)) { }

            BeamSupport support = new BeamSupport(point, isXFree, isYFree, isZFree, isRxFree, isRyFree, isRzFree);
            DA.SetData(0, new GH_BeamSupport(support));
        }
    }
}
