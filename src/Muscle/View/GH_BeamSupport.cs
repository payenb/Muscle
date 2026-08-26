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

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using MuscleApp.ViewModel;
using System;
using System.Drawing;

namespace Muscle.View
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial Element/Truss/DR/SRDR pipeline (see
    /// GH_Support). Grasshopper wrapper for MuscleApp.ViewModel.BeamSupport: draws a
    /// dark-orange cone per fixed translation (like GH_Support, but orange instead of blue to
    /// stay visually consistent with GH_BeamElement) plus a small dark-orange box per fixed
    /// rotation - the extra DOF that only exists in the Beam FE model.
    /// </summary>
    public class GH_BeamSupport : GH_GeometricGoo<BeamSupport>, IGH_PreviewData
    {
        public override bool IsValid { get { return new GH_Point(Value.Point).IsValid; } }

        public override string IsValidWhyNot
        {
            get
            {
                if (!new GH_Point(Value.Point).IsValid) { return "The point of application of the support is invalid."; }
                return string.Empty;
            }
        }

        public override string TypeDescription { get { return "A Beam Support may fix the X, Y, Z translations and/or Rx, Ry, Rz rotations of a Beam Node."; } }
        public override string TypeName { get { return "Beam Support"; } }
        public override BeamSupport Value { set; get; }

        public GH_BeamSupport()
        {
            Value = new BeamSupport();
        }

        public GH_BeamSupport(BeamSupport support) : base(support)
        {
            Value = support;
        }

        public GH_BeamSupport(GH_GeometricGoo<BeamSupport> gh_support)
        {
            Value = gh_support.Value;
        }

        public override bool CastFrom(object source)
        {
            if (source == null) { return false; }
            if (source is Point3d point)
            {
                Value.Point = point;
                return true;
            }
            if (source is GH_Point gh_point)
            {
                Value.Point = gh_point.Value;
                return true;
            }
            return base.CastFrom(source);
        }

        public override bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(BeamSupport)))
            {
                target = (Q)(object)Value;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(Point3d)))
            {
                target = (Q)(object)Value.Point;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
            {
                target = (Q)(object)new GH_Point(Value.Point);
                return true;
            }
            target = default;
            return false;
        }

        #region Display
        private static double height = 2.5 * MuscleConfig.DisplaySupportAmpli;
        private static double radius = 1.0 * MuscleConfig.DisplaySupportAmpli;
        private static double boxSize = 1.5 * MuscleConfig.DisplaySupportAmpli;
        private static double size = Math.Max(height, radius);

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            height = 2.5 * MuscleConfig.DisplaySupportAmpli;
            radius = 1.0 * MuscleConfig.DisplaySupportAmpli;
            boxSize = 1.5 * MuscleConfig.DisplaySupportAmpli;

            if (!Value.isXFree) { args.Pipeline.DrawCone(new Cone(new Plane(Value.Point, new Vector3d(-1.0, 0.0, 0.0)), height, radius), Color.DarkOrange); }
            if (!Value.isYFree) { args.Pipeline.DrawCone(new Cone(new Plane(Value.Point, new Vector3d(0.0, -1.0, 0.0)), height, radius), Color.DarkOrange); }
            if (!Value.isZFree) { args.Pipeline.DrawCone(new Cone(new Plane(Value.Point, new Vector3d(0.0, 0.0, -1.0)), height, radius), Color.DarkOrange); }

            // BEAM-ONLY: small boxes mark fixed rotations, offset along each axis so they
            // don't overlap the translation cones.
            double off = height * 1.3;
            if (!Value.isRxFree) { DrawFixedRotationBox(args, Value.Point + new Vector3d(off, 0, 0)); }
            if (!Value.isRyFree) { DrawFixedRotationBox(args, Value.Point + new Vector3d(0, off, 0)); }
            if (!Value.isRzFree) { DrawFixedRotationBox(args, Value.Point + new Vector3d(0, 0, off)); }
        }

        private void DrawFixedRotationBox(GH_PreviewMeshArgs args, Point3d center)
        {
            var box = new Box(new Plane(center, Vector3d.ZAxis), new Interval(-boxSize / 2, boxSize / 2),
                new Interval(-boxSize / 2, boxSize / 2), new Interval(-boxSize / 2, boxSize / 2));
            args.Pipeline.DrawBox(box, Color.DarkOrange);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPoint(Value.Point, Color.DarkOrange);
        }

        public override BoundingBox Boundingbox
        {
            get
            {
                size = Math.Max(height, radius);
                Point3d point = Value.Point;
                BoundingBox boundingBox = new BoundingBox(point, point);
                boundingBox.Inflate(size * 2); // extra room for the rotation-fixity boxes
                return boundingBox;
            }
        }

        public BoundingBox ClippingBox { get { return Boundingbox; } }
        public bool Hidden { get; set; }
        public bool IsPreviewCapable { get { return true; } }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_BeamSupport(this);
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            size = Math.Max(height, radius);
            Point3d point = Value.Point;
            point.Transform(xform);
            BoundingBox boundingBox = new BoundingBox(point, point);
            boundingBox.Inflate(size * 2);
            return boundingBox;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            GH_BeamSupport morphed = new GH_BeamSupport(this);
            morphed.Value.Point = xmorph.MorphPoint(Value.Point);
            return morphed;
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            GH_BeamSupport transformed = new GH_BeamSupport(this);
            transformed.Value.Point.Transform(xform);
            return transformed;
        }
        #endregion

        public override object ScriptVariable()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
