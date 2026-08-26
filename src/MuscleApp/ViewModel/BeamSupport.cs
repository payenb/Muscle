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

using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace MuscleApp.ViewModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial Element/Truss/DR/SRDR pipeline.
    ///
    /// A support/boundary condition for the Beam FE model: 6 DOF (3 translations + 3
    /// rotations), unlike the 3-DOF, translation-only Support used everywhere else in
    /// Muscle. Mirrors Support's structure exactly, extended to the 3 extra rotational DOF.
    /// </summary>
    public class BeamSupport
    {
        #region Properties

        /// <summary>True if point is free to move in X dir. False if fixed.</summary>
        public bool isXFree { get; set; }
        /// <summary>True if point is free to move in Y dir. False if fixed.</summary>
        public bool isYFree { get; set; }
        /// <summary>True if point is free to move in Z dir. False if fixed.</summary>
        public bool isZFree { get; set; }

        /// <summary>BEAM-ONLY DOF. True if point is free to rotate about X. False if fixed.</summary>
        public bool isRxFree { get; set; }
        /// <summary>BEAM-ONLY DOF. True if point is free to rotate about Y. False if fixed.</summary>
        public bool isRyFree { get; set; }
        /// <summary>BEAM-ONLY DOF. True if point is free to rotate about Z. False if fixed.</summary>
        public bool isRzFree { get; set; }

        /// <summary>
        /// Return the number (0 to 6) of fixed DOF: 3 translations + 3 (beam-only) rotations.
        /// </summary>
        public int FixationCount
        {
            get
            {
                return Convert.ToInt16(!isXFree) + Convert.ToInt16(!isYFree) + Convert.ToInt16(!isZFree)
                     + Convert.ToInt16(!isRxFree) + Convert.ToInt16(!isRyFree) + Convert.ToInt16(!isRzFree);
            }
        }

        /// <summary>
        /// Point of application of the support
        /// </summary>
        public Point3d Point { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// The default constructor creates a support on the default point and fixes all 6 DOF.
        /// </summary>
        public BeamSupport()
        {
            Point = new Point3d();
            isXFree = false; isYFree = false; isZFree = false;
            isRxFree = false; isRyFree = false; isRzFree = false;
        }

        /// <summary>
        /// The constructor creates a support on a given point and fixes all 6 DOF.
        /// </summary>
        public BeamSupport(Point3d point)
        {
            Point = point;
            isXFree = false; isYFree = false; isZFree = false;
            isRxFree = false; isRyFree = false; isRzFree = false;
        }

        /// <summary>
        /// The constructor creates a support on a given point and fixes the given DOF.
        /// </summary>
        public BeamSupport(Point3d point, bool Xfree, bool Yfree, bool Zfree, bool RxFree, bool RyFree, bool RzFree)
        {
            Point = point;
            isXFree = Xfree; isYFree = Yfree; isZFree = Zfree;
            isRxFree = RxFree; isRyFree = RyFree; isRzFree = RzFree;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public BeamSupport(BeamSupport support)
        {
            Point = support.Point;
            isXFree = support.isXFree; isYFree = support.isYFree; isZFree = support.isZFree;
            isRxFree = support.isRxFree; isRyFree = support.isRyFree; isRzFree = support.isRzFree;
        }

        /// <summary>
        /// Return a new support with merged fixation conditions across all 6 DOF according to
        /// support1 and 2. If Support 1 and 2 are not defined on the same point, the merged
        /// support is defined on point1.
        /// </summary>
        public static BeamSupport MergeConditionsOf(BeamSupport support1, BeamSupport support2)
        {
            BeamSupport merged_support = support1.Duplicate();
            merged_support.isXFree = support1.isXFree && support2.isXFree;
            merged_support.isYFree = support1.isYFree && support2.isYFree;
            merged_support.isZFree = support1.isZFree && support2.isZFree;
            merged_support.isRxFree = support1.isRxFree && support2.isRxFree;
            merged_support.isRyFree = support1.isRyFree && support2.isRyFree;
            merged_support.isRzFree = support1.isRzFree && support2.isRzFree;
            return merged_support;
        }

        #endregion Constructors

        #region Methods

        public BeamSupport Duplicate()
        {
            return new BeamSupport(this);
        }

        /// <summary>
        /// Return human readable text describing the support.
        /// </summary>
        public override string ToString()
        {
            if (FixationCount == 0)
            {
                return $"Point [{Point}] is free to move (Beam support)!";
            }

            string message = $"Point [{Point}] is fixed in the ";
            if (!isXFree) { message += "X "; }
            if (!isYFree) { message += "Y "; }
            if (!isZFree) { message += "Z "; }
            if (!isRxFree) { message += "Rx "; }
            if (!isRyFree) { message += "Ry "; }
            if (!isRzFree) { message += "Rz "; }

            if (FixationCount == 1) { return message + "direction."; }
            else { return message + "directions."; }
        }

        /// <summary>
        /// Return a list of supports which have only one support per point. The fixing
        /// conditions are merged (across all 6 DOF) if there are different supports defined
        /// on the same point in the inputted list.
        /// </summary>
        public static List<BeamSupport> MergeConditionsOnSamePoint(List<BeamSupport> supports)
        {
            List<BeamSupport> merged_supports = new List<BeamSupport>();
            List<Point3d> points = new List<Point3d>();

            foreach (BeamSupport current_support in supports)
            {
                Point3d current_point = current_support.Point;
                int ind;
                if (Node.EpsilonContains(points, current_point, 1e-5f, out ind))
                {
                    BeamSupport duplicated_support = merged_supports[ind];
                    BeamSupport merged_support = MergeConditionsOf(duplicated_support, current_support);
                    points.RemoveAt(ind);
                    merged_supports.RemoveAt(ind);
                    points.Add(current_point);
                    merged_supports.Add(merged_support);
                }
                else
                {
                    points.Add(current_point);
                    merged_supports.Add(current_support);
                }
            }
            return merged_supports;
        }

        #endregion Methods
    }
}
