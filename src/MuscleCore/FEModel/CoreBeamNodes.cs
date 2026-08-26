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

namespace MuscleCore.FEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial DR/SRDR pipeline (see CoreNodes).
    ///
    /// Pure data transfer object for the Beam FE model's nodes: 6 DOF per node (3
    /// translations + 3 rotations), mirroring musclepy.solvers.beam_fem.PyBeamNodes. DOF
    /// order per node is [Tx, Ty, Tz, Rx, Ry, Rz].
    /// </summary>
    public class CoreBeamNodes
    {
        #region Properties

        /// <summary>[m] - shape (nodes_count, 3) - Initial nodal coordinates</summary>
        public double[,] InitialCoordinates { get; set; }

        /// <summary>[m] - shape (nodes_count, 3) - Current nodal coordinates</summary>
        public double[,] Coordinates { get; set; }

        /// <summary>[-] - shape (nodes_count, 6) - True if free, False if fixed, order [Tx,Ty,Tz,Rx,Ry,Rz]</summary>
        public bool[,] DOF { get; set; }

        /// <summary>Number of nodes</summary>
        public int Count { get; set; }

        /// <summary>[N,N,N,N.m,N.m,N.m] - shape (nodes_count, 6) - External loads applied to nodes</summary>
        public double[,] Loads { get; set; }

        /// <summary>[m,m,m,rad,rad,rad] - shape (nodes_count, 6) - Nodal displacements</summary>
        public double[,] Displacements { get; set; }

        /// <summary>[N,N,N,N.m,N.m,N.m] - shape (nodes_count, 6) - Support reactions</summary>
        public double[,] Reactions { get; set; }

        /// <summary>[N,N,N,N.m,N.m,N.m] - shape (nodes_count, 6) - Internal resisting forces</summary>
        public double[,] ResistingForces { get; set; }

        /// <summary>[N,N,N,N.m,N.m,N.m] - shape (nodes_count, 6) - Out of balance loads</summary>
        public double[,] Residuals { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Minimal constructor, used to encode a request to Python.
        /// </summary>
        public CoreBeamNodes(double[,] initialCoordinates, bool[,] dof, double[,]? loads = null,
            double[,]? displacements = null, double[,]? reactions = null, double[,]? resistingForces = null)
        {
            InitialCoordinates = initialCoordinates ?? throw new ArgumentNullException(nameof(initialCoordinates));
            DOF = dof ?? throw new ArgumentNullException(nameof(dof));
            Count = initialCoordinates.GetLength(0);

            Coordinates = new double[Count, 3];
            Loads = loads ?? new double[Count, 6];
            Displacements = displacements ?? new double[Count, 6];
            Reactions = reactions ?? new double[Count, 6];
            ResistingForces = resistingForces ?? new double[Count, 6];
            Residuals = new double[Count, 6];
        }

        /// <summary>
        /// Full constructor, used when decoding a solved state from Python.
        /// </summary>
        public CoreBeamNodes(double[,] initialCoordinates, double[,] coordinates, bool[,] dof, int count,
            double[,] loads, double[,] displacements, double[,] reactions, double[,] resistingForces, double[,] residuals)
        {
            InitialCoordinates = initialCoordinates ?? throw new ArgumentNullException(nameof(initialCoordinates));
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
            DOF = dof ?? throw new ArgumentNullException(nameof(dof));
            Count = count;
            Loads = loads ?? throw new ArgumentNullException(nameof(loads));
            Displacements = displacements ?? throw new ArgumentNullException(nameof(displacements));
            Reactions = reactions ?? throw new ArgumentNullException(nameof(reactions));
            ResistingForces = resistingForces ?? throw new ArgumentNullException(nameof(resistingForces));
            Residuals = residuals ?? throw new ArgumentNullException(nameof(residuals));
        }

        #endregion Constructors
    }
}
