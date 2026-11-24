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
using System.Collections.Generic;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Muscle.View;
using static Muscle.Components.GHComponentsFolders;

namespace Muscle.Components.Display
{
    /// <summary>
    /// Shows the second-order bending moment or amplified deflection of struts, as computed
    /// by the Stiffness-Reduction Dynamic Relaxation solver (Eq. 12-13 of Payen B., Latteur P.,
    /// Pacheco de Almeida J., "Effect of Strut Geometric Imperfections on the Internal Forces
    /// Distribution in Tensegrity Structures", J. Struct. Eng., 2026, 152(5): 04026039).
    ///
    /// Mirrors ShowForces' structure/conventions (color gradient + line weight by magnitude,
    /// same Value Settings/Text Size pattern) so both display components feel consistent, but
    /// draws a curve offset from each straight Line rather than colouring the Line itself:
    /// each strut is assumed to bow along a half-sine between its end nodes (0 at both ends,
    /// the peak Value at mid-span) - the same shape musclepy.solvers.srdr assumes for the
    /// imperfection e0 and its amplification. This already produces a proper 3-point (end -
    /// centre - end) diagram, ready to extend to a genuine multi-station diagram once a
    /// beam-element solver can provide more than one interior value per element.
    /// </summary>
    public class ShowBending : GH_Component
    {
        #region Constructors
        public ShowBending()
          : base("ShowBending", "ShowB",
              "Show the second-order bending moment or amplified deflection of struts",
              GHAssemblyName, Folder7_Display)
        {
            gradient_bending = new GH_Gradient_Bending();
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("6f9d3d0a-6e2a-4a7b-8f5b-6b8b2b6e2a4d"); }
        }

        protected override Bitmap Icon
        {
            get { return Muscle.View.IconLoader.Get("ShowBending"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "L", "Lines", GH_ParamAccess.list); //0
            pManager.AddNumberParameter("Values", "V", "Moment (kN.m) or Deflection (mm) at mid-span of each line, e.g. from Deconstruct Element", GH_ParamAccess.list); //1
            pManager.AddNumberParameter("Multiplier", "x", "Exaggeration factor applied to Values to make the curve visible", GH_ParamAccess.item, 10.0); //2
            pManager.AddNumberParameter("Angle", "a (deg)", "Direction of the bow/diagram around each line's own axis, 0 to 360 degrees (0 = towards world Z, i.e. \"up\")", GH_ParamAccess.item, 0.0); //3
            pManager.AddIntegerParameter("Thickness", "t", "Maximum thickness of the curves. All curves have the same thickness if t=0.", GH_ParamAccess.item, 2); //4
            pManager.AddIntegerParameter("Value Settings", "v", "values to show (0 = no value ; 1 = extremes only ; 2 = all values)", GH_ParamAccess.item, 2); //5
            pManager.AddNumberParameter("Text Size", "size", "Size of the text", GH_ParamAccess.item, 0.1); //6
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        #endregion Constructors

        #region Properties

        // results
        private List<Line> lines;
        private List<GH_Number> GH_values;
        private List<double> values;
        private double multiplier;
        private double angleDegrees;
        private double valueMin, valueAbsMax;

        // per-line drawing data
        private List<Point3d[]> curves; // sampled, offset curve points per line
        private List<double> ColorParams; // 0 (white) to 1 (max magnitude)
        private List<int> WeightParams;

        // thickness
        private int user_thick_max;
        private int thick_max;
        private int thick_min = 3;

        // tags
        private List<int> ind_extremes;
        private int v_setting;
        private int d_setting = MuscleConfig.DisplayDecimals;
        private double text_size = 10;

        private readonly GH_Gradient_Bending gradient_bending;

        private const int N_SAMPLES = 25; // odd, so t=0.5 (mid-span) is sampled exactly

        #endregion Properties

        #region Methods

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            lines = new List<Line>();
            GH_values = new List<GH_Number>();

            if (!DA.GetDataList(0, lines)) { return; }
            if (!DA.GetDataList(1, GH_values)) { return; }
            if (!DA.GetData(2, ref multiplier)) { }
            if (!DA.GetData(3, ref angleDegrees)) { }
            if (!DA.GetData(4, ref user_thick_max)) { }
            if (!DA.GetData(5, ref v_setting)) { }
            if (!DA.GetData(6, ref text_size)) { }

            if (user_thick_max < 0) { user_thick_max = 0; }
            thick_max = user_thick_max + thick_min;

            if (v_setting < 0) { v_setting = 0; }
            if (v_setting > 2) { v_setting = 2; }

            InitCurvesColorsWeightsTags();
        }

        /// <summary>
        /// Build the offset curve points for every line, and map their peak (mid-span) value
        /// to colors/weights/extreme-tag indices - mirrors ShowForces.InitColorsWeightsTags.
        /// </summary>
        public void InitCurvesColorsWeightsTags()
        {
            double angleRad = angleDegrees * Math.PI / 180.0;

            values = new List<double>();
            valueMin = 0;
            valueAbsMax = 0;
            foreach (GH_Number gh_value in GH_values)
            {
                double value = (gh_value == null) ? double.NaN : (double)gh_value.Value;
                values.Add(value);
                if (double.IsNaN(value)) continue;
                if (value < valueMin) { valueMin = value; }
                if (Math.Abs(value) > valueAbsMax) { valueAbsMax = Math.Abs(value); }
            }

            ind_extremes = new List<int>();
            curves = new List<Point3d[]>();
            ColorParams = new List<double>();
            WeightParams = new List<int>();

            int count = Math.Min(lines.Count, values.Count);
            for (int i = 0; i < count; i++)
            {
                double value = values[i];

                if (!double.IsNaN(value) && (value == valueAbsMax || (value == valueMin && valueMin < 0)))
                {
                    ind_extremes.Add(i);
                }

                curves.Add(BuildCurvePoints(lines[i], double.IsNaN(value) ? 0.0 : value, angleRad));
                ColorParams.Add(ValueToColorParam(value));
                WeightParams.Add(ValueToWeightParam(value));
            }
        }

        /// <summary>
        /// Samples N_SAMPLES points along the line, each offset perpendicular to the line's
        /// own axis by a half-sine profile peaking at mid-span - the shape
        /// musclepy.solvers.srdr assumes for a strut's imperfection/deflection, and (since
        /// M = N*e_max scales the same profile) for its second-order moment too.
        /// </summary>
        private Point3d[] BuildCurvePoints(Line line, double value, double angleRad)
        {
            Vector3d axis = line.Direction;
            if (!axis.Unitize()) { axis = Vector3d.XAxis; }

            Vector3d reference = Vector3d.ZAxis;
            double dot = reference * axis;
            reference = reference - dot * axis; // project world Z onto the plane perpendicular to the axis
            if (!reference.Unitize())
            {
                // axis is parallel to world Z - fall back to world X as the 0-degree reference
                reference = Vector3d.XAxis;
                dot = reference * axis;
                reference = reference - dot * axis;
                reference.Unitize();
            }
            reference.Rotate(angleRad, axis); // user-controlled direction around the line's own axis

            Point3d[] points = new Point3d[N_SAMPLES];
            for (int k = 0; k < N_SAMPLES; k++)
            {
                double t = k / (double)(N_SAMPLES - 1);
                double amplitude = value * Math.Sin(Math.PI * t) * multiplier;
                points[k] = line.PointAt(t) + amplitude * reference;
            }
            return points;
        }

        /// <summary>
        /// Map a magnitude (Moment/Deflection have no sign the way tension/compression do) to
        /// a number between 0 (white) and 1 (orange).
        /// </summary>
        public double ValueToColorParam(double value)
        {
            if (valueAbsMax <= 0 || double.IsNaN(value)) { return 0.0; }
            return Math.Abs(value) / valueAbsMax;
        }

        /// <summary>
        /// Map a magnitude to an integer thickness between thick_min and thick_max.
        /// </summary>
        public int ValueToWeightParam(double value)
        {
            if (valueAbsMax <= 0 || double.IsNaN(value)) { return thick_min; }
            double param = Math.Abs(value) / valueAbsMax; // 0 to 1
            double amplified_param = thick_min + param * user_thick_max;
            int ceil = (int)Math.Ceiling(amplified_param);
            int floor = (int)Math.Floor(amplified_param);
            if (amplified_param - floor >= 0.5) { return ceil; }
            return floor;
        }

        /// <summary>
        /// Show, on a Tag, the value at mid-span of curve i.
        /// </summary>
        public void DrawTag(IGH_PreviewArgs args, int i)
        {
            if (curves == null || values == null) return;

            Point3d midpoint = curves[i][N_SAMPLES / 2]; // t=0.5 sample - the peak of the curve

            Plane plane;
            args.Viewport.GetCameraFrame(out plane);
            plane.Origin = midpoint;

            double value = Math.Round(values[i], d_setting, MidpointRounding.AwayFromZero);
            string txt = value.ToString();

            args.Display.Draw3dText(txt, Color.DarkOrange, plane, text_size, "Lucida Console");
        }

        /// <summary>
        /// Draw each line's straight baseline (thin, grey) and its offset moment/deflection
        /// curve (coloured/weighted by magnitude).
        /// </summary>
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            GH_Gradient gradient = gradient_bending.gradient;
            if (lines == null || curves == null || ColorParams == null || WeightParams == null) return;

            for (var i = 0; i < curves.Count; i++)
            {
                try
                {
                    args.Display.DrawLine(lines[i], Color.LightGray, 1);
                    args.Display.DrawPolyline(curves[i], gradient.ColourAt(ColorParams[i]), WeightParams[i]);
                }
                catch
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Draw the Tags showing the moment/deflection values (0 = no value ; 1 = extremes only ; 2 = all values).
        /// </summary>
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            d_setting = MuscleConfig.DisplayDecimals;

            if (v_setting == 0) { return; }
            else if (v_setting == 1)
            {
                for (int e = 0; e < ind_extremes.Count; e++)
                {
                    DrawTag(args, ind_extremes[e]);
                }
            }
            else
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    DrawTag(args, i);
                }
            }
        }

        #endregion Methods
    }
}
