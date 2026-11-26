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
// This fork (Muscle 2.0): modified by Basile Payen (UCLouvain) with Claude (Anthropic).
// ------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuscleApp.ViewModel;
using MuscleCore.PythonNETInit;
using Rhino.Geometry;

namespace MuscleCoreTests.Solvers
{
    /// <summary>
    /// Element masses of the Grasshopper modal analysis: when none are given, they are computed from
    /// the elements (free length x area x specific mass), as the component's input says.
    /// </summary>
    [TestClass]
    public class DynamicElementMassesTests
    {
        [TestInitialize]
        public void Initialize()
        {
            PythonNETManager.Launch(CoreTestsConfig.testConfig);
            Assert.IsTrue(PythonNETManager.IsInitialized);
        }

        [TestCleanup]
        public void Cleanup()
        {
            PythonNETManager.ShutDown();
        }

        /// <summary>Two aluminium bars from pinned supports to an apex free in x and z.</summary>
        private static Truss TwoBarTruss()
        {
            var cs = new CS_Circular(0.060, 0.002);
            var material = new BilinearMaterial("Aluminum", 70390e6, 70390e6, -160e6, 160e6, 2700.0);
            var a = new Point3d(0, 0, 0);
            var b = new Point3d(2, 0, 0);
            var apex = new Point3d(1, 0, 1);
            var elements = new List<Element>
            {
                new Element(new Line(a, apex), cs, material, "bar", "Yielding", 1.0),
                new Element(new Line(b, apex), cs, material, "bar", "Yielding", 1.0),
            };
            var supports = new List<Support>
            {
                new Support(a, false, false, false),
                new Support(b, false, false, false),
                new Support(apex, true, false, true),
            };
            return new Truss(elements, new List<Point3d>(), supports);
        }

        [TestMethod]
        public void EmptyElementMasses_AreComputedFromTheElements()
        {
            Truss truss = TwoBarTruss();
            double[] expected = truss.Elements.Select(e => e.Mass).ToArray();
            Assert.IsTrue(expected.All(m => m > 0));

            var automatic = MuscleApp.Solvers.DynamicModalAnalysis.Solve(truss, new List<PointMass>(), new List<double>(), 1, 0);
            var explicitMasses = MuscleApp.Solvers.DynamicModalAnalysis.Solve(truss, new List<PointMass>(), expected.ToList(), 1, 0);

            Assert.IsNotNull(automatic);
            CollectionAssert.AreEqual(explicitMasses.Frequencies, automatic.Frequencies);
            Assert.IsTrue(automatic.Frequencies[0] > 0);
        }
    }
}
