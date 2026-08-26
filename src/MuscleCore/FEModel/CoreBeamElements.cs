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
    /// BEAM FE MODEL ONLY - not used by the axial DR/SRDR pipeline (see CoreElements).
    ///
    /// Pure data transfer object for the Beam FE model's elements: 2-node, 6-DOF-per-node
    /// beam-columns with per-end hinge releases, mirroring
    /// musclepy.solvers.beam_fem.PyBeamElements. All units SI (m, Pa, N, N.m).
    /// </summary>
    public class CoreBeamElements
    {
        #region Properties

        /// <summary>CoreBeamNodes instance containing node data</summary>
        public CoreBeamNodes Nodes { get; set; }

        /// <summary>[-] - shape (elements_count, 2) - Indices of end nodes</summary>
        public int[,] EndNodes { get; set; }

        /// <summary>Number of elements</summary>
        public int Count { get; set; }

        /// <summary>[m²] - shape (elements_count,) - Cross-section area</summary>
        public double[]? Area { get; set; }

        /// <summary>[Pa] - shape (elements_count,) - Young's modulus (single value, not bilinear - see BeamElement)</summary>
        public double[]? Youngs { get; set; }

        /// <summary>[m4] - shape (elements_count,) - Symmetric bending inertia (Iyy=Izz)</summary>
        public double[]? Inertia { get; set; }

        /// <summary>[m4] - shape (elements_count,) - Saint-Venant torsion constant</summary>
        public double[]? TorsionConstant { get; set; }

        /// <summary>[Pa] - shape (elements_count,) - Shear modulus</summary>
        public double[]? ShearModulus { get; set; }

        /// <summary>[m] - shape (elements_count,) - Free length</summary>
        public double[]? FreeLength { get; set; }

        /// <summary>[-] - shape (elements_count,) - True releases both bending DOF at the start node</summary>
        public bool[]? HingeStart { get; set; }

        /// <summary>[-] - shape (elements_count,) - True releases both bending DOF at the end node</summary>
        public bool[]? HingeEnd { get; set; }

        /// <summary>
        /// [-] - shape (elements_count, 3) - Reference vector for cross-section orientation. A
        /// row of NaN means "automatic" (world Z, or world Y if colinear with the element axis).
        /// </summary>
        public double[,]? Orientation { get; set; }

        /// <summary>[N] - shape (elements_count,) - Axial force (positive in tension) - result</summary>
        public double[]? Tension { get; set; }

        /// <summary>[N.m] - shape (elements_count, 2) - [My, Mz] at the start node - result</summary>
        public double[,]? MomentStart { get; set; }

        /// <summary>[N.m] - shape (elements_count, 2) - [My, Mz] at the end node - result</summary>
        public double[,]? MomentEnd { get; set; }

        /// <summary>[N.m] - shape (elements_count,) - Torsion moment - result</summary>
        public double[]? Torsion { get; set; }

        #endregion Properties

        #region Constructors

        public CoreBeamElements(CoreBeamNodes nodes, int[,]? endNodes = null, double[]? area = null,
            double[]? youngs = null, double[]? inertia = null, double[]? torsionConstant = null,
            double[]? shearModulus = null, double[]? freeLength = null, bool[]? hingeStart = null,
            bool[]? hingeEnd = null, double[,]? orientation = null, double[]? tension = null,
            double[,]? momentStart = null, double[,]? momentEnd = null, double[]? torsion = null)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            EndNodes = endNodes ?? throw new ArgumentNullException(nameof(endNodes));
            Count = EndNodes.GetLength(0);

            Area = area;
            Youngs = youngs;
            Inertia = inertia;
            TorsionConstant = torsionConstant;
            ShearModulus = shearModulus;
            FreeLength = freeLength;
            HingeStart = hingeStart;
            HingeEnd = hingeEnd;
            Orientation = orientation;

            Tension = tension;
            MomentStart = momentStart;
            MomentEnd = momentEnd;
            Torsion = torsion;
        }

        #endregion Constructors
    }
}
