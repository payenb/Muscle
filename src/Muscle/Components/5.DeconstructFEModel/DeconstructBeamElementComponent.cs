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
using MuscleApp.ViewModel;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.DeconstructFEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - see DeconstructElementComponent for the axial equivalent. Unlike
    /// that component (a single mid-span Moment/Deflection from SRDR's closed-form
    /// amplification), this one exposes the real FE result: bending moments about both local
    /// axes AT EACH END separately, plus torsion.
    /// </summary>
    public class DeconstructBeamElementComponent : GH_Component
    {
        public DeconstructBeamElementComponent()
          : base("Deconstruct Beam Element", "DeBeamElem",
              "BEAM FE MODEL ONLY. Deconstruct a beam element to retrieve its properties, including end forces/moments from the Beam FEM solver.",
              GHAssemblyName, Folder5_DeconstructFEM)
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Beam Element", "BE", "Input beam element", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "name", "Beam element name", GH_ParamAccess.item); //0
            pManager.AddIntegerParameter("Element Index", "Idx", "Index of the beam element", GH_ParamAccess.item); //1
            pManager.AddIntegerParameter("End nodes indexes", "End nodes", "Indexes of the end nodes", GH_ParamAccess.list); //2
            pManager.AddLineParameter("Line", "L", "Current line and current length of the beam element", GH_ParamAccess.item); //3
            pManager.AddNumberParameter("Free length", "L0 (m)", "Free length of the beam element", GH_ParamAccess.item); //4
            pManager.AddGenericParameter("Cross Section", "CS", "Cross section of the beam element", GH_ParamAccess.item); //5
            pManager.AddGenericParameter("Material", "M", "Bilinear Material of the beam element", GH_ParamAccess.item); //6
            pManager.AddNumberParameter("Mass", "m (kg)", "Mass (kg) of the beam element", GH_ParamAccess.item); //7
            pManager.AddNumberParameter("Tension", "N (kN)", "Axial force (kN, tension positive, compression negative)", GH_ParamAccess.item); //8
            pManager.AddIntervalParameter("Resistances", "[tmin, tmax] (kN)", "Resistance interval [(-)Buckling resistance, (+)Yielding resistance] (kN)", GH_ParamAccess.item); //9
            pManager.AddNumberParameter("My Start", "MyS (kN.m)", "Bending moment about local y at the start node", GH_ParamAccess.item); //10
            pManager.AddNumberParameter("Mz Start", "MzS (kN.m)", "Bending moment about local z at the start node", GH_ParamAccess.item); //11
            pManager.AddNumberParameter("My End", "MyE (kN.m)", "Bending moment about local y at the end node", GH_ParamAccess.item); //12
            pManager.AddNumberParameter("Mz End", "MzE (kN.m)", "Bending moment about local z at the end node", GH_ParamAccess.item); //13
            pManager.AddNumberParameter("Torsion", "T (kN.m)", "Torsion moment", GH_ParamAccess.item); //14
            pManager.AddBooleanParameter("Hinge Start", "HingeS", "True if bending moments are released at the start node", GH_ParamAccess.item); //15
            pManager.AddBooleanParameter("Hinge End", "HingeE", "True if bending moments are released at the end node", GH_ParamAccess.item); //16
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            BeamElement e = new BeamElement();

            if (!DA.GetData(0, ref e)) { return; }

            DA.SetData(0, e.Name);
            DA.SetData(1, e.Idx);
            DA.SetDataList(2, e.EndNodes);
            DA.SetData(3, e.Line);
            DA.SetData(4, e.FreeLength);
            DA.SetData(5, e.CS);
            DA.SetData(6, e.Material);
            DA.SetData(7, e.Mass);
            DA.SetData(8, e.Tension / 1000);

            double Ryb = e.Resistance.T0 / 1000;
            double Ryt = e.Resistance.T1 / 1000;
            DA.SetData(9, new Rhino.Geometry.Interval(Ryb, Ryt));

            DA.SetData(10, e.MyStart / 1000);
            DA.SetData(11, e.MzStart / 1000);
            DA.SetData(12, e.MyEnd / 1000);
            DA.SetData(13, e.MzEnd / 1000);
            DA.SetData(14, e.Torsion / 1000);
            DA.SetData(15, e.HingeStart);
            DA.SetData(16, e.HingeEnd);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Muscle.View.IconLoader.Get("DeconstructBeamElement"); }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8a4c1e6f-3d9b-4c7a-8e1d-4f6a3c9b2e5d"); }
        }
    }
}
