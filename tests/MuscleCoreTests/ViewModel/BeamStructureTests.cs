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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuscleApp.ViewModel;
using Rhino.Geometry;

namespace MuscleCoreTests.ViewModel
{
    /// <summary>
    /// Tests for the Beam FE model's ViewModel scaffolding (BeamNode/BeamSupport/
    /// BeamElement/BeamStructure) - the new, 6-DOF, parallel pipeline that does not touch
    /// Element/Node/Support/Truss. Mirrors the registration behaviour already proven for
    /// Truss (node dedup, element-end wiring, support merging), plus BeamElement-specific
    /// additions (hinges, orientation, duplicated buckling-law logic).
    /// </summary>
    [TestClass]
    public class BeamStructureTests
    {
        private static BeamElement BuildAluminumBeamElement(Line line, string bucklingLaw = "Yielding",
            bool hingeStart = false, bool hingeEnd = false)
        {
            var cs = new CS_Circular(0.060, 0.002);
            var material = new BilinearMaterial("Aluminum", 70390e6, 70390e6, -160e6, 160e6, 2700.0);
            return new BeamElement(line, cs, material, "beam", bucklingLaw, 1.0, hingeStart, hingeEnd);
        }

        [TestMethod]
        public void BeamStructure_TwoCollinearElements_ShareTheMiddleNode()
        {
            // Two 3m elements sharing a middle node: (0,0,0)-(3,0,0)-(6,0,0). Mirrors how
            // Truss dedups shared element endpoints into a single Node.
            var line1 = new Line(new Point3d(0, 0, 0), new Point3d(3, 0, 0));
            var line2 = new Line(new Point3d(3, 0, 0), new Point3d(6, 0, 0));
            var elements = new List<BeamElement> { BuildAluminumBeamElement(line1), BuildAluminumBeamElement(line2) };

            var structure = new BeamStructure(elements, new List<Point3d>(), new List<BeamSupport>());

            Assert.AreEqual(3, structure.Nodes.Count);
            Assert.AreEqual(2, structure.Elements.Count);
            CollectionAssert.AreEqual(new List<int> { 0, 1 }, elements[0].EndNodes);
            CollectionAssert.AreEqual(new List<int> { 1, 2 }, elements[1].EndNodes);
        }

        [TestMethod]
        public void BeamStructure_RegisterSupports_FixesAllSixDofAtSupportedNode()
        {
            var line = new Line(new Point3d(0, 0, 0), new Point3d(3, 0, 0));
            var elements = new List<BeamElement> { BuildAluminumBeamElement(line) };
            var supports = new List<BeamSupport> { new BeamSupport(new Point3d(0, 0, 0), false, false, false, false, false, false) };

            var structure = new BeamStructure(elements, new List<Point3d>(), supports);

            BeamNode fixedNode = structure.Nodes[0];
            Assert.AreEqual(6, fixedNode.FixationsCount);
            Assert.IsFalse(fixedNode.isXFree);
            Assert.IsFalse(fixedNode.isRzFree);

            BeamNode freeNode = structure.Nodes[1];
            Assert.AreEqual(0, freeNode.FixationsCount);
        }

        [TestMethod]
        public void BeamStructure_RegisterSupports_MergesRestrictiveConditionOnSharedNode()
        {
            // Same point supported twice: once free in Rz, once fixed in Rz -> fixed wins,
            // same AND-merge rule as Node.AddSupport / Support.MergeConditionsOf.
            var line = new Line(new Point3d(0, 0, 0), new Point3d(3, 0, 0));
            var elements = new List<BeamElement> { BuildAluminumBeamElement(line) };
            var supports = new List<BeamSupport>
            {
                new BeamSupport(new Point3d(0, 0, 0), false, false, false, true, true, true), // pinned (rotations free)
                new BeamSupport(new Point3d(0, 0, 0), true, true, true, false, true, true)      // Rx fixed only
            };

            var structure = new BeamStructure(elements, new List<Point3d>(), supports);

            BeamNode node = structure.Nodes[0];
            Assert.IsFalse(node.isXFree); // fixed by support 1
            Assert.IsFalse(node.isRxFree); // fixed by support 2
            Assert.IsTrue(node.isRyFree); // free in both -> stays free
        }

        [TestMethod]
        public void BeamElement_HingeAndOrientation_DefaultToFalseAndNull()
        {
            var e = new BeamElement();
            Assert.IsFalse(e.HingeStart);
            Assert.IsFalse(e.HingeEnd);
            Assert.IsNull(e.Orientation);
        }

        [TestMethod]
        public void BeamElement_Xsi_Imperfection_MatchesElementHandCalc_ForSameArticleStrut()
        {
            // Same case as ElementBucklingTests.Xsi_Imperfection_MatchesHandCalc_ForArticleStrut
            // - confirms the duplicated buckling-law logic gives the identical result.
            var line = new Line(new Point3d(0, 0, 0), new Point3d(3.0, 0, 0));
            var e = BuildAluminumBeamElement(line, "Imperfection");

            Assert.AreEqual(0.172, e.Xsi, 0.005);
        }
    }
}
