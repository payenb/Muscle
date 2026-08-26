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
    /// A 2-node, 6-DOF-per-node beam-column element: unlike the purely axial (pin-jointed)
    /// Element used by DR/SRDR, a BeamElement carries real bending/shear/torsion stiffness.
    /// Per-end hinge releases (HingeStart/HingeEnd) let the same element represent either a
    /// genuine rigid/semi-rigid beam or a pin-jointed truss member (both ends released) -
    /// the same unification Karamba uses for its "Bar" elements, and the technique the
    /// thesis's Load_control.py already validates via rotule_start/rotule_end.
    ///
    /// The buckling-law logic (Xsi/Strength/Resistance, including the "Imperfection"/Eq. 14
    /// check) is intentionally DUPLICATED from Element.cs rather than shared, to keep the
    /// existing, already-validated Element class completely untouched, per the Beam FE
    /// model's "new parallel pipeline, don't perturb DR/SRDR" design.
    /// </summary>
    public class BeamElement
    {
        #region Properties
        public int Idx { get; set; } //index of the element in the structure
        public string TypeName { get { return "Beam Element"; } }
        public string Name { get; set; } = "General Beam Element";
        public Line Line { get; set; } = new Line();
        public List<int> EndNodes { get; set; } = new List<int>(); //index of the end nodes of the element
        public double FreeLength { get; set; } = -1.0; // [m]
        public virtual ICrossSection CS { get; set; } = new CS_Circular();
        public virtual BilinearMaterial Material { get; set; } = new BilinearMaterial();

        /// <summary>
        /// BEAM-ONLY. True releases both bending DOF at the line's start node (a pin/hinge),
        /// matching the thesis's rotule_start. False = rigidly connected to the node's
        /// rotation.
        /// </summary>
        public bool HingeStart { get; set; }

        /// <summary>
        /// BEAM-ONLY. True releases both bending DOF at the line's end node (a pin/hinge),
        /// matching the thesis's rotule_end. False = rigidly connected to the node's
        /// rotation.
        /// </summary>
        public bool HingeEnd { get; set; }

        /// <summary>
        /// BEAM-ONLY. Reference vector used to orient the cross-section's principal axes
        /// around the element's own axis. Null = automatic (world Z, or world Y if colinear
        /// with the element axis) - same convention as the thesis's Load_control.py v_ref.
        /// </summary>
        public Vector3d? Orientation { get; set; }

        public double V { get { return FreeLength * CS.Area; } } //m3
        public double Mass { get { return V * Material.Rho; } } //kg
        public Vector3d Weight { get { return MuscleAppConfig.g * Mass; } } //N

        public bool IsValid
        {
            get
            {
                return CS.IsValid && Material.IsValid && Line.IsValid
                    && Strength.T1 >= 0 && Strength.T0 <= 0
                    && !(Strength.T0 == 0.0 && Strength.T1 == 0.0);
            }
        }

        /// <summary>Axial force [N], set by the Beam FE solver.</summary>
        public double Tension { get; set; }

        /// <summary>[N.m] Bending moment about local y at the start node, set by the Beam FE solver.</summary>
        public double MyStart { get; set; }

        /// <summary>[N.m] Bending moment about local z at the start node, set by the Beam FE solver.</summary>
        public double MzStart { get; set; }

        /// <summary>[N.m] Bending moment about local y at the end node, set by the Beam FE solver.</summary>
        public double MyEnd { get; set; }

        /// <summary>[N.m] Bending moment about local z at the end node, set by the Beam FE solver.</summary>
        public double MzEnd { get; set; }

        /// <summary>[N.m] Torsion moment, set by the Beam FE solver.</summary>
        public double Torsion { get; set; }

        ///// Strength - duplicated from Element.cs, see class docstring for why /////

        public string BucklingLaw { get; set; } = "Yielding";
        public double kb { get; set; } = 1.0; //buckling factor

        public double Lambda
        {
            get
            {
                double Lb = kb * FreeLength;
                double A = CS.Area;
                double I = CS.Inertia;
                return Lb * Math.Sqrt(A / I);
            }
        }

        public double Lambda_E
        {
            get
            {
                double Ec = Material.Ec;
                double Fy = -Material.Fyc;
                return Math.PI * Math.Sqrt(Ec / Fy);
            }
        }

        public double Lambda_Adim { get { return Lambda / Lambda_E; } }

        private double Alpha
        {
            get
            {
                switch (BucklingLaw)
                {
                    case "a": return 0.21;
                    case "b": return 0.34;
                    case "c": return 0.49;
                    case "d": return 0.76;
                }
                return 0;
            }
        }

        private double Phi
        {
            get { return 0.5 * (1 + Alpha * (Lambda_Adim - 0.2) + Math.Pow(Lambda_Adim, 2)); }
        }

        private double Xsi_EC3
        {
            get
            {
                double xsi = 1 / (Phi + Math.Sqrt(Math.Pow(Phi, 2) - Math.Pow(Lambda_Adim, 2)));
                if (xsi <= 1) return xsi;
                else if (xsi > 1.0) return 1.0;
                else return 0.0;
            }
        }

        public double Xsi
        {
            get
            {
                switch (BucklingLaw)
                {
                    case "Euler":
                        {
                            double xsi = 1 / Math.Pow(Lambda_Adim, 2);
                            if (xsi <= 1) return xsi;
                            else return 1.0;
                        }
                    case "Rankine":
                        return 1 / (1 + Math.Pow(Lambda_Adim, 2));
                    case "a": return Xsi_EC3;
                    case "b": return Xsi_EC3;
                    case "c": return Xsi_EC3;
                    case "d": return Xsi_EC3;
                    case "Slack": return 0.0;
                    case "Imperfection": return Xsi_Imperfection;
                }
                return 1.0; // return yielding in all other cases !
            }
        }

        /// <summary>
        /// Same Ayrton-Perry/Perry-Robertson first-yield check as Element.Xsi_Imperfection
        /// (Eq. 14 of Payen B., Latteur P., Pacheco de Almeida J., "Effect of Strut Geometric
        /// Imperfections on the Internal Forces Distribution in Tensegrity Structures", J.
        /// Struct. Eng., 2026, 152(5): 04026039) - see that class for the derivation.
        /// </summary>
        private double Xsi_Imperfection
        {
            get
            {
                double A = CS.Area;
                double I = CS.Inertia;
                double Wel = CS.Wel;
                double Lb = FreeLength; // pinned-pinned; see Element.Xsi_Imperfection docstring
                double Ec = Material.Ec;
                double FycAbs = -Material.Fyc;
                if (A <= 0 || I <= 0 || Wel <= 0 || Lb <= 0 || Ec <= 0 || FycAbs <= 0)
                    return 0.0;

                double Ncr = Math.PI * Math.PI * Ec * I / (Lb * Lb);
                double e0 = FreeLength / 300.0; // Eurocode 9 default

                double sigmaCr = Ncr / A;
                double eta = A * e0 / Wel;
                double sum = (1 + eta) + FycAbs / sigmaCr;
                double discriminant = sum * sum - 4 * FycAbs / sigmaCr;
                if (discriminant < 0) discriminant = 0;

                double n = (sum - Math.Sqrt(discriminant)) / 2.0;
                double xsi = n * sigmaCr / FycAbs;

                if (xsi < 0) return 0.0;
                if (xsi > 1) return 1.0;
                return xsi;
            }
        }

        protected double Fyb { get { return Xsi * Material.Fyc; } }

        public Interval Strength { get { return new Interval(Fyb, Material.Fyt); } }

        public Interval Resistance
        {
            get
            {
                double Fyt = Strength.T1;
                double aFyb = Strength.T0;
                return new Interval(aFyb * CS.Area, Fyt * CS.Area);
            }
        }

        #endregion Properties

        #region Constructors

        private void Init()
        {
            Idx = -1;
            Name = "General Beam Element";
            Line = new Line();
            FreeLength = -1.0;
            EndNodes = new List<int>();
            CS = new CS_Circular();
            Material = new BilinearMaterial();
            HingeStart = false;
            HingeEnd = false;
            Orientation = null;
            BucklingLaw = "Yielding";
            kb = 1.0;
            Tension = 0;
            MyStart = 0;
            MzStart = 0;
            MyEnd = 0;
            MzEnd = 0;
            Torsion = 0;
        }

        public BeamElement() { Init(); }

        public BeamElement(Line aLine, ICrossSection aCS, BilinearMaterial aMat, string name,
            string buckling_law, double buckling_factor, bool hingeStart = false, bool hingeEnd = false,
            Vector3d? orientation = null)
        {
            Init();
            Line = aLine;
            FreeLength = aLine.Length; // Use the length of the inputted line as the free length
            CS = aCS.Copy();
            Material = aMat.Copy();
            Name = name;
            BucklingLaw = buckling_law;
            kb = buckling_factor;
            if (kb <= 0) kb = 0;
            HingeStart = hingeStart;
            HingeEnd = hingeEnd;
            Orientation = orientation;
        }

        public BeamElement(BeamElement other) // Copy constructor
        {
            Name = other.Name;
            Line = other.Line;
            FreeLength = other.FreeLength;
            EndNodes = other.EndNodes;
            CS = other.CS.Copy();
            Material = other.Material.Copy();
            BucklingLaw = other.BucklingLaw;
            kb = other.kb;
            Idx = other.Idx;
            Tension = other.Tension;
            MyStart = other.MyStart;
            MzStart = other.MzStart;
            MyEnd = other.MyEnd;
            MzEnd = other.MzEnd;
            Torsion = other.Torsion;
            HingeStart = other.HingeStart;
            HingeEnd = other.HingeEnd;
            Orientation = other.Orientation;
        }

        #endregion Constructors

        #region Methods

        public BeamElement Copy()
        {
            return new BeamElement(this);
        }

        public override string ToString()
        {
            return $"{Name} {Idx} with free length {FreeLength:F3}m and mass {Mass:F1}kg. Hinges: start={HingeStart}, end={HingeEnd}.";
        }

        #endregion Methods
    }
}
