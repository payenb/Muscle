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
using MuscleCore.Solvers;
using MuscleCore.FEModel;
using MuscleCore.PythonNETInit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MuscleCoreTests.Solvers.BeamDR
{
    /// <summary>
    /// BEAM FE MODEL ONLY - headless (no Rhino/Grasshopper) integration tests for the
    /// explicit kinetic-damping Dynamic Relaxation Beam solver, going through the real
    /// C#/Python.NET bridge. Mirrors BeamFEMSolverTests' closed-form cases, plus a direct
    /// cross-check that BeamDR and BeamFEM (two independent numerical methods for the same
    /// equilibrium equations) agree on identical inputs - already proven in Python
    /// (scratchpad/validate_beam_dr.py); this confirms it holds through the C# bridge too.
    /// </summary>
    [TestClass]
    public class BeamDRSolverTests
    {
        [TestInitialize]
        public void Initialize()
        {
            string anacondaPath = PythonNETConfig.TryFindingAnaconda();
            var testConfig = new PythonNETConfig(anacondaPath, "muscle-dev", string.Empty, string.Empty);
            Assert.IsTrue(testConfig.IsValid);

            PythonNETManager.Launch(testConfig);
            Assert.IsTrue(PythonNETManager.IsInitialized);
        }

        [TestCleanup]
        public void Cleanup()
        {
            PythonNETManager.ShutDown();
        }

        private const double E = 200e9; // Pa (steel)
        private const double I = 8.36e-6; // m4
        private const double A = 5.0e-3; // m2
        private static readonly double G = E / (2 * (1 + 0.3));
        private static readonly double J = 2 * I;

        private static CoreBeamStructure BuildCantilever(double L, double P, out CoreBeamNodes nodes, out CoreBeamElements elements)
        {
            nodes = new CoreBeamNodes(
                initialCoordinates: new double[,] { { 0, 0, 0 }, { L, 0, 0 } },
                dof: new bool[,] { { false, false, false, false, false, false }, { true, true, true, true, true, true } },
                loads: new double[,] { { 0, 0, 0, 0, 0, 0 }, { 0, P, 0, 0, 0, 0 } }
            );
            elements = new CoreBeamElements(
                nodes: nodes,
                endNodes: new int[,] { { 0, 1 } },
                area: new double[] { A },
                youngs: new double[] { E },
                inertia: new double[] { I },
                torsionConstant: new double[] { J },
                shearModulus: new double[] { G }
            );
            return new CoreBeamStructure(nodes, elements);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_Cantilever_TipLoad_MatchesEulerBernoulli()
        {
            double L = 3.0;
            double P = 1000.0;
            var structure = BuildCantilever(L, P, out _, out _);
            var config = new CoreConfigBeamDR();

            var result = MuscleCore.Solvers.BeamDR.Solve(structure, config);

            Assert.IsNotNull(result);
            Assert.IsTrue(config.Converged, $"n_time_step={config.NTimeStep}, n_ke_reset={config.NKEReset}, residual={config.Residual}");

            double expectedDeflection = P * Math.Pow(L, 3) / (3 * E * I);
            double expectedMoment = P * L;
            double tipDeflection = Math.Abs(result.Nodes.Displacements[1, 1]);
            double fixedEndMoment = Math.Abs(result.Elements.MomentStart[0, 1]);

            Assert.AreEqual(expectedDeflection, tipDeflection, expectedDeflection * 1e-3, "tip deflection vs PL^3/3EI");
            Assert.AreEqual(expectedMoment, fixedEndMoment, expectedMoment * 1e-3, "fixed-end moment vs P*L");
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_PureAxialCompression_ZeroBending_CorrectStiffness()
        {
            double L = 3.0;
            double N = -8200.0;

            var nodes = new CoreBeamNodes(
                initialCoordinates: new double[,] { { 0, 0, 0 }, { L, 0, 0 } },
                dof: new bool[,] { { false, false, false, false, false, false }, { true, false, false, true, true, true } },
                loads: new double[,] { { 0, 0, 0, 0, 0, 0 }, { N, 0, 0, 0, 0, 0 } }
            );
            var elements = new CoreBeamElements(
                nodes: nodes,
                endNodes: new int[,] { { 0, 1 } },
                area: new double[] { A }, youngs: new double[] { E }, inertia: new double[] { I },
                torsionConstant: new double[] { J }, shearModulus: new double[] { G }
            );
            var structure = new CoreBeamStructure(nodes, elements);
            var config = new CoreConfigBeamDR();

            var result = MuscleCore.Solvers.BeamDR.Solve(structure, config);

            Assert.IsNotNull(result);
            Assert.IsTrue(config.Converged);

            double expectedAxialDisp = N * L / (E * A);
            Assert.AreEqual(N, result.Elements.Tension[0], Math.Abs(N) * 1e-3, "axial tension matches applied load");
            Assert.AreEqual(expectedAxialDisp, result.Nodes.Displacements[1, 0], Math.Abs(expectedAxialDisp) * 1e-3, "axial displacement vs N*L/(EA)");
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_HingeRelease_EliminatesMomentTransferThroughJoint()
        {
            double Lh = 2.0;
            var nodes = new CoreBeamNodes(
                initialCoordinates: new double[,] { { 0, 0, 0 }, { Lh, 0, 0 }, { 2 * Lh, 0, 0 } },
                dof: new bool[,] {
                    { false, false, false, false, false, false },
                    { true, true, true, true, true, true },
                    { false, false, false, false, false, false }
                },
                loads: new double[,] { { 0, 0, 0, 0, 0, 0 }, { 0, -500, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 } }
            );
            var elements = new CoreBeamElements(
                nodes: nodes,
                endNodes: new int[,] { { 0, 1 }, { 1, 2 } },
                area: new double[] { A, A }, youngs: new double[] { E, E }, inertia: new double[] { I, I },
                torsionConstant: new double[] { J, J }, shearModulus: new double[] { G, G },
                hingeStart: new bool[] { false, false },
                hingeEnd: new bool[] { true, false }
            );
            var structure = new CoreBeamStructure(nodes, elements);
            var config = new CoreConfigBeamDR();

            var result = MuscleCore.Solvers.BeamDR.Solve(structure, config);

            Assert.IsNotNull(result);
            Assert.IsTrue(config.Converged);

            double momentElem0AtMid = Math.Abs(result.Elements.MomentEnd[0, 1]);
            double momentElem1AtWall = Math.Abs(result.Elements.MomentEnd[1, 1]);

            Assert.IsTrue(momentElem0AtMid < 1e-3, $"hinged end should carry ~0 moment, got {momentElem0AtMid}");
            Assert.IsTrue(momentElem1AtWall > 1.0, $"fixed end should carry real moment, got {momentElem1AtWall}");
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_BeamDR_MatchesBeamFEM_OnIdenticalInputs()
        {
            // Two independent numerical methods (explicit mass-based DR vs. implicit
            // Newton-Raphson) solving the same equilibrium equations must agree - already
            // proven in Python (scratchpad/validate_beam_dr.py); this confirms it holds
            // through the C#/Python.NET bridge (separate encoders/decoders/config types) too.
            double L = 3.0;
            double P = 1000.0;

            var femStructure = BuildCantilever(L, P, out _, out _);
            var femResult = MuscleCore.Solvers.BeamFEM.Solve(femStructure, new CoreConfigBeam());

            var drStructure = BuildCantilever(L, P, out _, out _);
            var drResult = MuscleCore.Solvers.BeamDR.Solve(drStructure, new CoreConfigBeamDR());

            Assert.IsNotNull(femResult);
            Assert.IsNotNull(drResult);

            double femDeflection = femResult.Nodes.Displacements[1, 1];
            double drDeflection = drResult.Nodes.Displacements[1, 1];
            Assert.AreEqual(femDeflection, drDeflection, Math.Abs(femDeflection) * 1e-3, "tip deflection: DR vs FEM");

            double femMoment = femResult.Elements.MomentStart[0, 1];
            double drMoment = drResult.Elements.MomentStart[0, 1];
            Assert.AreEqual(femMoment, drMoment, Math.Abs(femMoment) * 1e-3, "fixed-end moment: DR vs FEM");

            double femTension = femResult.Elements.Tension[0];
            double drTension = drResult.Elements.Tension[0];
            Assert.AreEqual(femTension, drTension, Math.Max(Math.Abs(femTension) * 1e-2, 1.0), "2nd-order axial tension: DR vs FEM");
        }
    }
}
