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
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using MuscleApp.ViewModel;

namespace Muscle.View
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial Element/Truss/DR/SRDR pipeline (see
    /// GH_Element). Grasshopper wrapper for MuscleApp.ViewModel.BeamElement. Draws its line
    /// in dark orange by default (unlike GH_Element, which draws nothing) so Beam elements
    /// are visually distinguishable on the canvas from regular axial Elements.
    /// </summary>
    public class GH_BeamElement : GH_GeometricGoo<BeamElement>, IGH_PreviewData
    {
        public override BoundingBox Boundingbox
        {
            get
            {
                BoundingBox bBox = Value.Line.BoundingBox;
                if (Value.IsValid) { bBox.Inflate(Value.CS.Dimension * 0.75); }
                return bBox;
            }
        }

        public BoundingBox ClippingBox { get { return Boundingbox; } }
        public override string TypeDescription { get { return "A Beam Element is a 2-node, 6-DOF-per-node beam-column, for the Beam FE model."; } }
        public override string TypeName { get { return Value.TypeName; } }

        public override BeamElement Value
        {
            get { return base.Value; }
            set { base.Value = value; }
        }

        public GH_BeamElement()
        {
            Value = new BeamElement();
        }

        public GH_BeamElement(BeamElement e)
        {
            Value = e;
        }

        public GH_BeamElement(GH_Goo<BeamElement> gh_e)
        {
            Value = gh_e.Value;
        }

        public override bool CastFrom(object source)
        {
            if (source == null) { return false; }
            if (source is GH_Line gh_line)
            {
                Value.Line = gh_line.Value;
                return true;
            }
            if (source is BeamElement e)
            {
                Value = e;
                return true;
            }
            if (source is GH_ObjectWrapper wrapper && wrapper.Value is BeamElement wrapped)
            {
                Value = wrapped;
                return true;
            }
            return base.CastFrom(source);
        }

        public override bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Line)))
            {
                target = (Q)(object)Value.Line;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
            {
                target = (Q)(object)new GH_Line(Value.Line);
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(BeamElement)))
            {
                target = (Q)(object)Value;
                return true;
            }
            target = default;
            return false;
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args) { }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawLine(Value.Line, Color.DarkOrange, 2);
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_BeamElement(this);
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            if (Value == null) { return BoundingBox.Empty; }
            BoundingBox bBox = xform.TransformBoundingBox(Value.Line.BoundingBox);
            if (Value.IsValid) { bBox.Inflate(Value.CS.Dimension * 0.75); }
            return bBox;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            GH_BeamElement element = new GH_BeamElement(this);
            LineCurve lc = new LineCurve(element.Value.Line);
            xmorph.Morph(lc);
            element.Value.Line = lc.Line;
            return element;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            GH_BeamElement element = new GH_BeamElement(this);
            element.Value.Line.Transform(xform);
            return element;
        }
    }
}
