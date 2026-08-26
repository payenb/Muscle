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
    /// A node of the Beam FE model: 6 DOF (3 translations + 3 rotations), unlike the 3-DOF,
    /// translation-only Node used everywhere else in Muscle (DR, SRDR, and every other
    /// existing solver). Mirrors Node's structure exactly, extended to the 3 extra
    /// rotational DOF needed to carry bending/torsion.
    /// </summary>
    public class BeamNode
    {
        #region Properties
        public string TypeName { get { return "Beam Node"; } }
        public int Idx { get; set; } //index of the node in the structure

        public Point3d Coordinates { get; set; }
        public bool IsValid { get { return true; } }

        ///// Support informations /////
        #region Support

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
        public int FixationsCount
        {
            get
            {
                return Convert.ToInt16(!isXFree) + Convert.ToInt16(!isYFree) + Convert.ToInt16(!isZFree)
                     + Convert.ToInt16(!isRxFree) + Convert.ToInt16(!isRyFree) + Convert.ToInt16(!isRzFree);
            }
        }

        public Vector3d Reactions { get; set; } // force reactions [N]

        /// <summary>BEAM-ONLY. Moment reactions [N.m] about X, Y, Z.</summary>
        public Vector3d MomentReactions { get; set; }

        #endregion Support

        ///// Load informations /////
        #region Load

        public Vector3d Loads { get; set; } // force loads [N]

        /// <summary>BEAM-ONLY. Moment loads [N.m] about X, Y, Z.</summary>
        public Vector3d MomentLoads { get; set; }

        public Vector3d Residuals { get; set; } // unbalanced force

        /// <summary>BEAM-ONLY. Unbalanced moment [N.m].</summary>
        public Vector3d MomentResiduals { get; set; }

        #endregion Load

        #endregion Properties

        #region Constructors

        private void Init()
        {
            Idx = -1;
            Coordinates = new Point3d();

            isXFree = true; isYFree = true; isZFree = true;
            isRxFree = true; isRyFree = true; isRzFree = true;

            Reactions = new Vector3d();
            MomentReactions = new Vector3d();
            Loads = new Vector3d();
            MomentLoads = new Vector3d();
            Residuals = new Vector3d();
            MomentResiduals = new Vector3d();
        }

        public BeamNode() { Init(); }
        public BeamNode(Point3d point) { Init(); Coordinates = point; }
        public BeamNode(Point3d point, int idx) { Init(); Coordinates = point; Idx = idx; }

        public BeamNode(BeamNode other)
        {
            Idx = other.Idx;
            Coordinates = other.Coordinates;

            isXFree = other.isXFree; isYFree = other.isYFree; isZFree = other.isZFree;
            isRxFree = other.isRxFree; isRyFree = other.isRyFree; isRzFree = other.isRzFree;

            Reactions = other.Reactions;
            MomentReactions = other.MomentReactions;
            Loads = other.Loads;
            MomentLoads = other.MomentLoads;
            Residuals = other.Residuals;
            MomentResiduals = other.MomentResiduals;
        }

        public BeamNode Copy() { return new BeamNode(this); }

        #endregion Constructors

        #region Methods

        public override string ToString()
        {
            return $"Beam Node {Idx} with coordinates {Coordinates}";
        }

        /// <summary>
        /// Add the support conditions to the node, across all 6 DOF. If two supports are
        /// added to a same node, the more restraining condition (Fixed) is conserved - same
        /// merge rule as Node.AddSupport.
        /// </summary>
        public void AddSupport(BeamSupport support)
        {
            isXFree = isXFree && support.isXFree;
            isYFree = isYFree && support.isYFree;
            isZFree = isZFree && support.isZFree;
            isRxFree = isRxFree && support.isRxFree;
            isRyFree = isRyFree && support.isRyFree;
            isRzFree = isRzFree && support.isRzFree;
        }

        ///// Static Methods related to List<BeamNode> /////
        #region List<BeamNode>Methods

        /// <summary>
        /// Reuses Node's static point-dedup helper (pure geometry, no coupling to the 3-DOF
        /// pipeline) rather than duplicating it.
        /// </summary>
        public static bool EpsilonContains(List<BeamNode> nodes, Point3d thePoint, double ZeroTol, out int ind)
        {
            List<Point3d> points = nodes.Select(n => n.Coordinates).ToList();
            return Node.EpsilonContains(points, thePoint, ZeroTol, out ind);
        }

        #endregion List<BeamNode>Methods

        #endregion Methods
    }
}
