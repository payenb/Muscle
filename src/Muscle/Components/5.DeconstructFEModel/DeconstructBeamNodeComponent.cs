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
using MuscleApp.ViewModel;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.DeconstructFEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - see DeconstructNodeComponent for the axial (3-DOF) equivalent.
    /// </summary>
    public class DeconstructBeamNodeComponent : GH_Component
    {
        public DeconstructBeamNodeComponent()
          : base("Deconstruct Beam Node", "DeBeamNode",
              "BEAM FE MODEL ONLY. Deconstruct a beam node to retrieve its properties (6 DOF: 3 translations + 3 rotations).",
              GHAssemblyName, Folder5_DeconstructFEM)
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Node", "BN", "Input beam node", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Pt (m)", "Current node coordinates", GH_ParamAccess.item); //0
            pManager.AddIntegerParameter("Index", "Idx", "Node index", GH_ParamAccess.item); //1
            pManager.AddBooleanParameter("Is Free", "DoF", "Is it free to move/rotate in [X, Y, Z, Rx, Ry, Rz] ? True if free, False if fixed by a beam support.", GH_ParamAccess.list); //2
            pManager.AddVectorParameter("Load", "F (kN)", "Total forces applied on the node", GH_ParamAccess.item); //3
            pManager.AddVectorParameter("Moment Load", "M (kN.m)", "BEAM-ONLY. Total moments applied on the node", GH_ParamAccess.item); //4
            pManager.AddVectorParameter("Reactions", "R (kN)", "Reaction forces at the beam supports", GH_ParamAccess.item); //5
            pManager.AddVectorParameter("Moment Reactions", "MR (kN.m)", "BEAM-ONLY. Reaction moments at the beam supports", GH_ParamAccess.item); //6
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            BeamNode n = new BeamNode();

            if (!DA.GetData(0, ref n)) { return; }

            DA.SetData(0, n.Coordinates);
            DA.SetData(1, n.Idx);
            List<bool> IsFree = new List<bool> { n.isXFree, n.isYFree, n.isZFree, n.isRxFree, n.isRyFree, n.isRzFree };
            DA.SetDataList(2, IsFree);
            DA.SetData(3, n.Loads / 1000);
            DA.SetData(4, n.MomentLoads / 1000);
            DA.SetData(5, n.Reactions / 1000);
            DA.SetData(6, n.MomentReactions / 1000);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return null; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("5c9d3f7a-1e6b-4a8c-9d2f-7b3e1a6c4d8f"); }
        }
    }
}
