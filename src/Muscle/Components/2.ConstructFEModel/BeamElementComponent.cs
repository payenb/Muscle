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
    /// BEAM FE MODEL ONLY - constructs a MuscleApp.ViewModel.BeamElement (6-DOF beam-column),
    /// entirely separate from "Construct Element" (the axial, pin-jointed Element used by
    /// DR/SRDR). Reuses the same CrossSection/Material/Buckling Law inputs as "Construct
    /// Element" for consistency, plus 3 Beam-only inputs: Hinge Start, Hinge End (per-end
    /// moment releases - both False means a rigid beam, both True means a pin-jointed truss
    /// member) and (Orientation), the reference vector for the cross-section's local axes.
    /// </summary>
    public class BeamElementComponent : GH_Component
    {
        public BeamElementComponent()
          : base("Construct Beam Element", "BeamE",
              "BEAM FE MODEL ONLY. A Beam Element is a 2-node, 6-DOF-per-node beam-column, constructed from a line, a cross-section and a material - unlike Construct Element (axial only, for DR/SRDR), a Beam Element carries real bending/shear/torsion stiffness.",
              GHAssemblyName, Folder2_ConstructFEM)
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("1b6a1e3a-2f5e-4b0a-9b1a-6a5b9f3c7d2e"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return null; }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Line", "L", "Line defining the Beam Element's axis", GH_ParamAccess.item); //0
            pManager.AddGenericParameter("Cross Section", "CS", "Cross section of the beam element", GH_ParamAccess.item); //1
            pManager[1].Optional = true;
            pManager.AddGenericParameter("Material", "M", "Bilinear Material of the beam element", GH_ParamAccess.item); //2
            pManager[2].Optional = true;
            pManager.AddTextParameter("Element Name", "Name", "Name of the beam element", GH_ParamAccess.item, "Beam Element"); //3
            pManager[3].Optional = true;
            pManager.AddIntegerParameter("Buckling Law", "B. law", "Choose a buckling law between \"Euler\", \"Rankine\", \"Imperfection\", Eurocode (EN1993) curves \"a\",\"b\",\"c\",\"d\". \"Yielding\" law corresponds to buckling strength = yielding strength. \"Slack\" law cancels the strength in compression.", GH_ParamAccess.item, 0); //4
            pManager[4].Optional = true;
            pManager.AddNumberParameter("Buckling Factor", "k", "Buckling factor k defines the buckling length Lb such that Lb = k*Lfree", GH_ParamAccess.item, 1.0); //5
            pManager[5].Optional = true;
            pManager.AddBooleanParameter("Hinge Start", "HingeS", "BEAM-ONLY. True releases both bending moments at the line's start node (a pin/hinge). False = rigidly connected to the node's rotation.", GH_ParamAccess.item, false); //6
            pManager[6].Optional = true;
            pManager.AddBooleanParameter("Hinge End", "HingeE", "BEAM-ONLY. True releases both bending moments at the line's end node (a pin/hinge). False = rigidly connected to the node's rotation.", GH_ParamAccess.item, false); //7
            pManager[7].Optional = true;
            pManager.AddVectorParameter("(Orientation)", "(Zaxis)", "BEAM-ONLY. (Optional) Reference vector used to orient the cross-section's principal axes around the beam's own axis. If not connected: automatic (world Z, or world Y if colinear with the beam axis).", GH_ParamAccess.item); //8
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Element", "BE", "Beam Element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Line line = new Line();
            GH_CrossSection ghCS = new GH_CrossSection();
            GH_BilinearMaterial ghMat = new GH_BilinearMaterial();
            string name = "Beam Element";
            int lawIdx = 0;
            double k = 1.0;
            bool hingeStart = false;
            bool hingeEnd = false;
            Vector3d orientation = Vector3d.Unset;

            if (!DA.GetData(0, ref line)) { return; }
            if (!DA.GetData(1, ref ghCS)) { }
            if (!DA.GetData(2, ref ghMat)) { }
            if (!DA.GetData(3, ref name)) { }
            if (!DA.GetData(4, ref lawIdx)) { }
            if (!DA.GetData(5, ref k)) { }
            if (!DA.GetData(6, ref hingeStart)) { }
            if (!DA.GetData(7, ref hingeEnd)) { }
            bool hasOrientation = DA.GetData(8, ref orientation);

            int index = Array.IndexOf(ElementComponent._bucklingLawsIdx, lawIdx);
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please choose a valid Buckling Law from the provided input list");
                lawIdx = 0;
                index = Array.IndexOf(ElementComponent._bucklingLawsIdx, lawIdx);
            }
            string law = ElementComponent._bucklingLaws[index];

            Vector3d? orientationOrNull = (hasOrientation && orientation.IsValid && orientation != Vector3d.Unset) ? (Vector3d?)orientation : null;

            BeamElement e = new BeamElement(line, ghCS.Value, ghMat.Value, name, law, k, hingeStart, hingeEnd, orientationOrNull);
            GH_BeamElement gh_e = new GH_BeamElement(e);

            DA.SetData(0, gh_e);
        }
    }
}
