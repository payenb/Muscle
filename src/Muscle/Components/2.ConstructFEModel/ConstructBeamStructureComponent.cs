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
using System.IO;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Muscle.View;
using MuscleApp.ViewModel;
using Muscle.Converters;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.ConstructFEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - constructs a MuscleApp.ViewModel.BeamStructure from Beam
    /// Elements/Beam Supports, entirely separate from "Construct Structure" (the axial
    /// Truss, used by DR/SRDR). Mirrors that component's exact registration logic (see
    /// BeamStructure.cs), just operating on the 6-DOF Beam types.
    /// </summary>
    public class ConstructBeamStructureComponent : GH_Component
    {
        public ConstructBeamStructureComponent()
          : base("Construct Beam Structure", "BeamStruct",
              "BEAM FE MODEL ONLY. Creates a Beam Finite Element Model of the structure - unlike Construct Structure (axial only, for DR/SRDR), a Beam Structure's elements carry real bending/shear/torsion stiffness.",
              GHAssemblyName, Folder2_ConstructFEM)
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("4e8b2c6f-9a3d-4f7e-b1c5-8d2a6e9f3b4c"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Muscle.View.IconLoader.Get("BeamStructure"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Elements", "BE", "The beam finite elements.", GH_ParamAccess.tree);
            pManager.AddPointParameter("(Points)", "(Pt)", "(Optional) If no points are provided, the points are extracted from the beam elements ends in a random order", GH_ParamAccess.tree, null);
            pManager.AddGenericParameter("(Beam Supports)", "(BSpt)", "(Optional) Define the supports of the beam structure.", GH_ParamAccess.tree);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Structure", "BStruct", "A Beam Structure is made of Beam Nodes, Beam Elements, (Beam Supports)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> gh_elements = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Point> gh_points = new GH_Structure<GH_Point>();
            GH_Structure<IGH_Goo> gh_supports = new GH_Structure<IGH_Goo>();

            if (!DA.GetDataTree(0, out gh_elements)) { return; }
            if (!DA.GetDataTree(1, out gh_points)) { }
            if (!DA.GetDataTree(2, out gh_supports)) { gh_supports = null; }

            List<BeamElement> elements = GH_Decoders.ToBeamElementList(gh_elements);
            List<Point3d> points = GH_Decoders.ToPoint3dList(gh_points);
            List<BeamSupport> supports = GH_Decoders.ToBeamSupportList(gh_supports);

            BeamStructure structure = null;
            try
            {
                structure = new BeamStructure(elements, points, supports);
            }
            catch (InvalidDataException e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                return;
            }
            foreach (string warning in structure.warnings)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);
            }
            structure.warnings.Clear();

            DA.SetData(0, new GH_BeamStructure(structure));
        }
    }
}
