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
using System.Linq;

namespace MuscleApp.ViewModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial Element/Truss/DR/SRDR pipeline.
    ///
    /// The Beam FE model's structure container - mirrors Truss's exact 4-step registration
    /// (RegisterElements -> RegisterPointsAsNodes -> RegisterNodesAsElementsEnds ->
    /// RegisterSupports), operating on the 6-DOF BeamElement/BeamNode/BeamSupport instead of
    /// the 3-DOF Element/Node/Support. Entirely independent of Truss/DR/SRDR - a Beam
    /// Structure never touches, and is never touched by, the existing Truss pipeline.
    /// </summary>
    public class BeamStructure
    {
        #region Properties

        public string TypeName { get { return "Beam Structure"; } }

        /// <summary>List of warnings generated during structure initialization.</summary>
        public List<string> warnings { get; private set; } = new List<string>();

        /// <summary>
        /// Absolute Tolerance for geometric comparisons in [m] - same convention as
        /// Truss.ZeroGeometricATol.
        /// </summary>
        public double ZeroGeometricATol { get; private set; } = 1e-5f;

        public bool IsValid { get { return true; } }

        public List<BeamElement> Elements { get; set; } = new List<BeamElement>();
        public List<BeamNode> Nodes { get; set; } = new List<BeamNode>();
        public int FixationsCount { get { return Nodes.Select(n => n.FixationsCount).Sum(); } }

        public bool IsInEquilibrium { get; set; }

        #endregion Properties

        #region Constructors

        private void Init()
        {
            warnings = new List<string>();
            ZeroGeometricATol = 1e-5f; //(m)
            Elements = new List<BeamElement>();
            Nodes = new List<BeamNode>();
            IsInEquilibrium = false;
        }

        public BeamStructure() { Init(); }

        /// <summary>
        /// Creates a BeamStructure from lists of BeamElements, Points, and BeamSupports -
        /// same 4-step registration as Truss(elements, points, supports).
        /// </summary>
        public BeamStructure(List<BeamElement> elements, List<Point3d> points, List<BeamSupport> supports)
        {
            Init();

            RegisterElements(elements);
            RegisterPointsAsNodes(points);
            RegisterNodesAsElementsEnds();
            RegisterSupports(supports);
        }

        public BeamStructure(BeamStructure other)
        {
            warnings = new List<string>();
            ZeroGeometricATol = other.ZeroGeometricATol;

            Elements = new List<BeamElement>();
            foreach (BeamElement e in other.Elements) Elements.Add(e.Copy());

            Nodes = new List<BeamNode>();
            foreach (BeamNode n in other.Nodes) Nodes.Add(n.Copy());

            IsInEquilibrium = other.IsInEquilibrium;
        }

        public BeamStructure Copy()
        {
            return new BeamStructure(this);
        }

        #endregion Constructors

        #region Methods

        public override string ToString()
        {
            return $"Beam Structure of {Nodes.Count} nodes, {Elements.Count} elements, {FixationsCount} fixed DOF and {6 * Nodes.Count - FixationsCount} free degrees of freedom.";
        }

        #region 1)RegisterElements

        private void RegisterElements(List<BeamElement> elements)
        {
            int index = 0;
            foreach (var element in elements)
            {
                Elements.Add(element);
                element.Idx = index;
                index++;
            }
        }

        #endregion 1)RegisterElements

        #region 2)RegisterPointsAsNodes

        private void RegisterPointsAsNodes(List<Point3d> points)
        {
            int ind_node = 0;
            List<Point3d> points_from_lines = ElementsEndPoints();
            SpanXYZ(points_from_lines);
            List<Point3d> points_from_lines_wo_d = Node.RemoveDuplicatedPoints(points_from_lines, ZeroGeometricATol);

            if (!(points == null || points.Count == 0))
            {
                List<Point3d> points_input_wo_d = Node.RemoveDuplicatedPoints(points, ZeroGeometricATol);
                if (points_input_wo_d.Count < points.Count) { warnings.Add("Some user inputted points were duplicates and have been removed. Indexation of points may be affected."); }

                UserPointsContainsAllExtremities(points_input_wo_d, points_from_lines_wo_d);

                foreach (Point3d p in points_input_wo_d)
                {
                    Nodes.Add(new BeamNode(p, ind_node));
                    ind_node++;
                }
            }
            else
            {
                foreach (Point3d p in points_from_lines_wo_d)
                {
                    Nodes.Add(new BeamNode(p, ind_node));
                    ind_node++;
                }
            }
        }

        private List<Point3d> ElementsEndPoints()
        {
            List<Point3d> points = new List<Point3d>();
            foreach (BeamElement e in Elements)
            {
                points.Add(e.Line.From);
                points.Add(e.Line.To);
            }
            return points;
        }

        private void SpanXYZ(List<Point3d> points_from_lines)
        {
            if (points_from_lines.Count == 0)
            {
                ZeroGeometricATol = 1e-5f;
                return;
            }
            double minX = points_from_lines[0].X;
            double maxX = points_from_lines[0].X;
            double minY = points_from_lines[0].Y;
            double maxY = points_from_lines[0].Y;
            double minZ = points_from_lines[0].Z;
            double maxZ = points_from_lines[0].Z;

            foreach (Point3d point in points_from_lines)
            {
                if (point.X < minX) { minX = point.X; }
                if (point.X > maxX) { maxX = point.X; }
                if (point.Y < minY) { minY = point.Y; }
                if (point.Y > maxY) { maxY = point.Y; }
                if (point.Z < minZ) { minZ = point.Z; }
                if (point.Z > maxZ) { maxZ = point.Z; }
            }
            ZeroGeometricATol = Math.Max(Math.Abs(maxX - minX), Math.Max(Math.Abs(maxY - minY), Math.Abs(maxZ - minZ))) / 100000;
        }

        private void UserPointsContainsAllExtremities(List<Point3d> points_user, List<Point3d> extremities)
        {
            foreach (Point3d extremity in extremities)
            {
                int ind;
                if (!Node.EpsilonContains(points_user, extremity, ZeroGeometricATol, out ind))
                {
                    points_user.Add(extremity);
                    warnings.Add("Some line extremities were not contained in the user list of points. They have been added at the end of the user list of points");
                }
            }
            if (points_user.Count > extremities.Count) { warnings.Add("Some Points inputted by the user are not connected to the geometry"); }
        }

        #endregion 2)RegisterPointsAsNodes

        #region 3)RegisterNodesAsElementsEnds

        private void RegisterNodesAsElementsEnds()
        {
            foreach (BeamElement e in Elements)
            {
                Point3d n0 = e.Line.From;
                Point3d n1 = e.Line.To;

                int ind0 = -1;
                int ind1 = -1;
                for (int j = 0; j < Nodes.Count; j++)
                {
                    if (Nodes[j].Coordinates.EpsilonEquals(n0, ZeroGeometricATol)) { ind0 = j; }
                    if (Nodes[j].Coordinates.EpsilonEquals(n1, ZeroGeometricATol)) { ind1 = j; }
                }
                e.EndNodes = new List<int> { ind0, ind1 };
            }
        }

        #endregion 3)RegisterNodesAsElementsEnds

        #region 4)RegisterSupports

        private void RegisterSupports(List<BeamSupport> supports)
        {
            foreach (var support in supports)
            {
                int ind;
                if (BeamNode.EpsilonContains(Nodes, support.Point, ZeroGeometricATol, out ind))
                {
                    Nodes[ind].AddSupport(support);
                }
                else
                {
                    warnings.Add("A support is defined on a point which do not belong to the geometry. This support is ignored.");
                }
            }
        }

        #endregion 4)RegisterSupports

        #endregion Methods
    }
}
