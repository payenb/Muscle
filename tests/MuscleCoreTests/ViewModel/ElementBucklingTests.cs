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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuscleApp.ViewModel;
using Rhino.Geometry;

namespace MuscleCoreTests.ViewModel
{
    /// <summary>
    /// Tests for Element's "Imperfection" BucklingLaw: the Ayrton-Perry/Perry-Robertson
    /// first-yield capacity check (Eq. 14 of Payen B., Latteur P., Pacheco de Almeida J.,
    /// "Effect of Strut Geometric Imperfections on the Internal Forces Distribution in
    /// Tensegrity Structures", J. Struct. Eng., 2026, 152(5): 04026039), implemented as
    /// Element.Xsi_Imperfection.
    /// </summary>
    [TestClass]
    public class ElementBucklingTests
    {
        private static Element BuildAluminumStrut()
        {
            // Same aluminum strut as the article's worked example (Table 1): hollow circular,
            // outside diameter 60mm, thickness 2mm, E=70390 MPa, fy=160 MPa, L0~3m.
            var line = new Line(new Point3d(0, 0, 0), new Point3d(3.0, 0, 0));
            var cs = new CS_Circular(0.060, 0.002);
            var material = new BilinearMaterial("Aluminum", 70390e6, 70390e6, -160e6, 160e6, 2700.0);
            return new Element(line, cs, material, "strut", "Imperfection", buckling_factor: 1.0);
        }

        [TestMethod]
        public void Xsi_Imperfection_MatchesHandCalc_ForArticleStrut()
        {
            var e = BuildAluminumStrut();

            // Hand calc: sigma_cr = Ncr/A = 32.38 MPa, eta = A*e0/Wel = 0.713,
            // Fyc_abs/sigma_cr = 4.941 -> n = 0.851 -> Xsi = n*sigma_cr/Fyc_abs = 0.172
            Assert.AreEqual(0.172, e.Xsi, 0.005);
        }

        [TestMethod]
        public void Xsi_Imperfection_StaysBetweenEulerAndYieldCapacity()
        {
            var e = BuildAluminumStrut();
            double A = e.CS.Area;
            double FycAbs = -e.Material.Fyc;
            double Ncr = System.Math.PI * System.Math.PI * e.Material.Ec * e.CS.Inertia / (e.FreeLength * e.FreeLength);

            double capacityN = e.Xsi * FycAbs * A;

            // An imperfect strut can never sustain more than the pure Euler load, nor more
            // than the pure yield capacity - the whole point of the Ayrton-Perry check is that
            // both effects combine to reduce capacity below either limit alone.
            Assert.IsTrue(capacityN > 0);
            Assert.IsTrue(capacityN < Ncr, $"capacity {capacityN}N should be below pure Euler Ncr={Ncr}N");
            Assert.IsTrue(capacityN < FycAbs * A, $"capacity {capacityN}N should be below pure yield {FycAbs * A}N");
        }

        [TestMethod]
        public void Xsi_Imperfection_ResistanceMatchesCompressionSideOfResistanceInterval()
        {
            var e = BuildAluminumStrut();

            // Resistance.T0 (the "tmin" shown by Deconstruct Element) must be Xsi*Fyc*Area,
            // negative (compression sign convention), consistent with every other BucklingLaw.
            Assert.AreEqual(e.Xsi * e.Material.Fyc * e.CS.Area, e.Resistance.T0, 1.0);
            Assert.IsTrue(e.Resistance.T0 < 0);
        }
    }
}
