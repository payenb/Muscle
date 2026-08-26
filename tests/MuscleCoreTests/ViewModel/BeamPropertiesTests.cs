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

namespace MuscleCoreTests.ViewModel
{
    /// <summary>
    /// Tests for the torsion constant J (ICrossSection) and shear modulus G
    /// (BilinearMaterial), added for the Beam FE model - the axial-only
    /// Element/Truss/DR/SRDR pipeline never reads either property.
    /// </summary>
    [TestClass]
    public class BeamPropertiesTests
    {
        [TestMethod]
        public void CS_Circular_J_EqualsTwiceInertia_ExactForAnyCircularSection()
        {
            var cs = new CS_Circular(0.060, 0.002); // same hollow strut as ElementBucklingTests
            Assert.AreEqual(2.0 * cs.Inertia, cs.J, 1e-15);
        }

        [TestMethod]
        public void CS_Square_J_MatchesBredtThinWalledHandCalc()
        {
            // 100mm outer side, 5mm wall: J = (D-t)^3 * t = 0.095^3 * 0.005 = 4.286875e-6 m^4
            var cs = new CS_Square(0.100, 0.005);
            Assert.AreEqual(4.286875e-6, cs.J, 1e-11);
        }

        [TestMethod]
        public void BilinearMaterial_G_MatchesHandCalc_ForSteelWithDefaultPoisson()
        {
            // Steel, E=210000 MPa, default Poisson=0.3 -> G = E/(2*(1+0.3)) ~= 80769.2 MPa
            var steel = new BilinearMaterial("Steel", 210e9, 210e9, -235e6, 235e6, 7850.0);
            Assert.AreEqual(0.3, steel.Poisson, 1e-12);
            Assert.AreEqual(80769.23e6, steel.G, 1e6);
        }
    }
}
