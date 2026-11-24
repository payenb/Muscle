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

namespace MuscleCoreTests.Solvers.SRDR
{
    /// <summary>
    /// Headless (no Rhino/Grasshopper) regression tests for the Stiffness-Reduction Dynamic
    /// Relaxation solver, covering the DR->SRDR sequence within one Python.NET session, a
    /// single-strut minimal case, and the exact geometry/load that caught the area/youngs
    /// SI-unit bug (see musclepy.solvers.srdr.main.main_stiffness_reduction_dr).
    /// </summary>
    [TestClass]
    public class SRDRSolverTests
    {
        [TestInitialize]
        public void Initialize()
        {
            // Target muscle-dev specifically (not CoreTestsConfig's "base" env) to faithfully
            // reproduce the exact environment/numpy version used from Grasshopper. IMPORTANT:
            // MuscleApp.PythonNETInit.PythonNET.TryConfiguring only passes a non-empty
            // srcDirectory (developer mode) when the conda env name is literally the magic
            // string "muscle4developers" - any other env name, including "muscle-dev", goes
            // through USER mode (srcDir = empty), relying entirely on muscle-dev's own
            // editable-install .pth mechanism. Match that exactly here.
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

        // SI, like everything musclepy receives: MuscleApp copies Element.CS.Area [m^2],
        // Element.Material.Ec [Pa] and Element.CS.Inertia [m^4] unconverted. The data below are
        // written in mm and MPa for readability and converted once here.
        private const double MM2_TO_M2 = 1e-6;
        private const double MM4_TO_M4 = 1e-12;
        private const double MPA_TO_PA = 1e6;

        private static CoreTruss BuildSimplex(double strutYoungsPa = 70390 * MPA_TO_PA, double strutAreaM2 = 364.4 * MM2_TO_M2)
        {
            // Same aluminum simplex geometry as MusclePyTests.solvers.srdr.test_srdr_simplex,
            // with the strut cross-section area / Young's modulus parameterized for sweeping.
            double mm = 1e-3; // mm -> m
            var nodes = new CoreNodes(
                initialCoordinates: new double[,] {
                    { 0.00 * mm, -2043.82 * mm, 0.00 * mm },
                    { 0.00 * mm, 0.00 * mm, 0.00 * mm },
                    { 1770.00 * mm, -1021.91 * mm, 0.00 * mm },
                    { 590.00 * mm, -2201.91 * mm, 1950.00 * mm },
                    { -431.91 * mm, -431.91 * mm, 1950.00 * mm },
                    { 1611.91 * mm, -431.91 * mm, 1950.00 * mm }
                },
                dof: new bool[,] {
                    { false, true, false },
                    { false, false, false },
                    { true, true, false },
                    { true, true, true },
                    { true, true, true },
                    { true, true, true }
                }
            );

            double cableArea = 50.3 * MM2_TO_M2;
            double cableYoungs = 71750 * MPA_TO_PA;

            var elements = new CoreElements(
                nodes: nodes,
                type: new int[] { -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                endNodes: new int[,] {
                    { 2, 4 }, { 1, 3 }, { 0, 5 },
                    { 1, 2 }, { 0, 1 }, { 0, 2 }, { 4, 5 }, { 3, 4 }, { 3, 5 }, { 2, 5 }, { 1, 4 }, { 0, 3 }
                },
                area: new double[] {
                    strutAreaM2, strutAreaM2, strutAreaM2,
                    cableArea, cableArea, cableArea, cableArea, cableArea, cableArea, cableArea, cableArea, cableArea
                },
                youngs: new double[,] {
                    { strutYoungsPa, 0 }, { strutYoungsPa, 0 }, { strutYoungsPa, 0 },
                    { 0, cableYoungs }, { 0, cableYoungs }, { 0, cableYoungs }, { 0, cableYoungs }, { 0, cableYoungs },
                    { 0, cableYoungs }, { 0, cableYoungs }, { 0, cableYoungs }, { 0, cableYoungs }
                }
            );

            return new CoreTruss(nodes, elements);
        }

        /// <summary>[m^4] Inertia of the 3 simplex struts (hollow circular, radii Ro and Ri in mm), 0 for the cables.</summary>
        private static double[] Inertia(double Ro = 30.0, double Ri = 28.0)
        {
            double strutInertia = Math.PI / 4 * (Math.Pow(Ro, 4) - Math.Pow(Ri, 4)) * MM4_TO_M4;
            return new double[] { strutInertia, strutInertia, strutInertia, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }

        private static double CircularHollowArea(double Ro, double Ri) => Math.PI * (Ro * Ro - Ri * Ri);

        [TestMethod]
        public void Test_DRThenSRDR_SameSession_LowLoad()
        {
            var structure = BuildSimplex();
            double[] loads = new double[18]; // 6 nodes * 3 dof
            loads[2] = 45.7; loads[5] = 45.7; loads[8] = 45.7; // nodes 0-2, Z
            loads[11] = 41.6; loads[14] = 41.6; loads[17] = 41.6; // nodes 3-5, Z

            double[] deltaFreeLength = new double[12];
            deltaFreeLength[0] = 0.835e-3; deltaFreeLength[1] = 0.835e-3; deltaFreeLength[2] = 0.835e-3;
            deltaFreeLength[8] = -35e-3;

            var drConfig = new CoreConfigDR(dt: 0.01, massAmplFactor: 1.0, minMass: 0.005,
                maxTimeStep: 1000, maxKEResets: 100, zeroResidualRTol: 1e-4, zeroResidualATol: 1e-6);

            // 1) Plain DR first - mirrors "DR classique fonctionne"
            var drResult = DynamicRelaxation.Solve(structure, loads, deltaFreeLength, drConfig);
            Assert.IsNotNull(drResult);

            // 2) SRDR on the SAME structure, in the SAME Python.NET session (no re-Launch) -
            // mirrors "puis on active SRDR sans redémarrer Python.NET"
            var srdrConfig = new CoreConfigSRDR(forceTol: 1.0, maxOuterIterations: 100);
            var srdrDrConfig = new CoreConfigDR(dt: 0.01, massAmplFactor: 1.0, minMass: 0.005,
                maxTimeStep: 1000, maxKEResets: 100, zeroResidualRTol: 1e-4, zeroResidualATol: 1e-6);

            var srdrResult = StiffnessReductionDynamicRelaxation.Solve(
                structure, loads, deltaFreeLength, srdrDrConfig, srdrConfig, Inertia(), null);

            Assert.IsNotNull(srdrResult);
            Assert.IsTrue(srdrConfig.Converged, $"SRDR did not converge within {srdrConfig.MaxOuterIterations} iterations");
        }

        [TestMethod]
        public void Test_SRDR_AfterReinitCycle_StillFindsPyConfigSRDR()
        {
            // Defensive check for "Start Python.NET -> toggle off -> toggle on again" within
            // the same Rhino session: CPython does not officially support repeated
            // Py_Initialize/Py_Finalize cycles in one process, so this confirms a second Launch
            // after a Shutdown still leaves musclepy fully usable.
            PythonNETManager.ShutDown();
            Assert.IsFalse(PythonNETManager.IsInitialized);

            string anacondaPath = PythonNETConfig.TryFindingAnaconda();
            var testConfig = new PythonNETConfig(anacondaPath, "muscle-dev", string.Empty, string.Empty);
            PythonNETManager.Launch(testConfig);
            Assert.IsTrue(PythonNETManager.IsInitialized, "Second Launch after Shutdown failed");

            var structure = BuildSimplex();
            double[] loads = new double[18];
            loads[2] = 45.7; loads[5] = 45.7; loads[8] = 45.7;
            loads[11] = 41.6; loads[14] = 41.6; loads[17] = 41.6;
            double[] deltaFreeLength = new double[12];
            deltaFreeLength[0] = 0.835e-3; deltaFreeLength[1] = 0.835e-3; deltaFreeLength[2] = 0.835e-3;
            deltaFreeLength[8] = -35e-3;

            var drConfig = new CoreConfigDR(dt: 0.01, massAmplFactor: 1.0, minMass: 0.005,
                maxTimeStep: 1000, maxKEResets: 100, zeroResidualRTol: 1e-4, zeroResidualATol: 1e-6);
            var srdrConfig = new CoreConfigSRDR(forceTol: 1.0, maxOuterIterations: 100);

            var srdrResult = StiffnessReductionDynamicRelaxation.Solve(
                structure, loads, deltaFreeLength, drConfig, srdrConfig, Inertia(), null);

            Assert.IsNotNull(srdrResult);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_SRDR_SingleStrut_DefaultsOnly_NearZeroLoad()
        {
            // Mirrors the user's minimal repro: a structure with a SINGLE bar, only
            // Structure + Load connected in Grasshopper (everything else - Imperfection
            // Ratio, tolerances, warm start, ... - left at its default). All my other tests
            // use 3 struts; a single-strut structure exercises the is_strut boolean mask
            // and per-strut arrays (lam, ncr, imperfection_ratio, ...) with only one True
            // entry, which could hit a numpy edge case (scalar-like 1-element arrays) that
            // never shows up with 3+ struts.
            var nodes = new CoreNodes(
                initialCoordinates: new double[,] {
                    { 0.0, 0.0, 0.0 },
                    { 2.0, 0.0, 0.0 }
                },
                dof: new bool[,] {
                    { false, false, false },
                    { true, false, false }
                }
            );

            double Ro = 30.0, Ri = 28.0; // mm
            double strutArea = CircularHollowArea(Ro, Ri) * MM2_TO_M2; // m^2
            var elements = new CoreElements(
                nodes: nodes,
                type: new int[] { -1 },
                endNodes: new int[,] { { 0, 1 } },
                area: new double[] { strutArea },
                youngs: new double[,] { { 70390 * MPA_TO_PA, 0 } }
            );

            var structure = new CoreTruss(nodes, elements);

            // Near-zero axial compressive load, exactly as the user described
            double[] loads = new double[6]; // 2 nodes * 3 dof, node-major: [n0.x,n0.y,n0.z, n1.x,n1.y,n1.z]
            loads[3] = -0.001; // tiny compressive push on node 1 (the free node), x direction

            double[] deltaFreeLength = new double[1]; // no prestress

            var drConfig = new CoreConfigDR(); // production defaults, nothing else connected
            var srdrConfig = new CoreConfigSRDR(); // production defaults

            double singleStrutInertia = Math.PI / 4 * (Math.Pow(Ro, 4) - Math.Pow(Ri, 4)) * MM4_TO_M4; // m^4
            var result = StiffnessReductionDynamicRelaxation.Solve(
                structure, loads, deltaFreeLength, drConfig, srdrConfig, new double[] { singleStrutInertia }, null);

            Assert.IsNotNull(result);
            Assert.IsFalse(double.IsNaN(result.Elements.Tension[0]));
            Assert.IsTrue(srdrConfig.Converged);
            Console.WriteLine($"outerIter={srdrConfig.NOuterIterations}, tension={result.Elements.Tension[0]}, " +
                $"isInEquilibrium={result.IsInEquilibrium}");
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_SRDR_SingleStrut_RealLoad_ExplicitImperfection_MatchesTheory()
        {
            // A single 3m strut, D=60mm t=2mm, E=70000 MPa, 8.2kN axial compression, explicit
            // e0/L0=0.00333 (Eurocode 9). Area/youngs are SI (m^2/Pa) - matching exactly what
            // MuscleApp.Converters.ElementsEncoder produces from Grasshopper - so this pins the
            // theoretically correct elongation (hand calc: Kred/K=0.544 at N/Ncr=0.696 =>
            // ~1.77mm, vs ~0.96mm for a perfectly straight strut under the same load).
            var nodes = new CoreNodes(
                initialCoordinates: new double[,] {
                    { 0.0, 0.0, 0.0 },
                    { 3.0, 0.0, 0.0 }
                },
                dof: new bool[,] {
                    { false, false, false },
                    { true, false, false }
                }
            );

            double Ro = 30.0, Ri = 28.0; // mm
            double strutArea = CircularHollowArea(Ro, Ri) * MM2_TO_M2; // m^2
            var elements = new CoreElements(
                nodes: nodes,
                type: new int[] { -1 },
                endNodes: new int[,] { { 0, 1 } },
                area: new double[] { strutArea },
                youngs: new double[,] { { 70000 * MPA_TO_PA, 0 } }
            );
            var structure = new CoreTruss(nodes, elements);

            double[] loads = new double[6]; // 2 nodes * 3 dof, node-major: [n0.x,n0.y,n0.z, n1.x,n1.y,n1.z]
            loads[3] = -8200.0; // N, on node 1 (the free node), matching the user's 8.20 kN screenshot

            double[] deltaFreeLength = new double[1];

            var drConfig = new CoreConfigDR();
            var srdrConfig = new CoreConfigSRDR();
            double strutInertia = Math.PI / 4 * (Math.Pow(Ro, 4) - Math.Pow(Ri, 4)) * MM4_TO_M4; // m^4

            // Plain DR baseline
            var drResult = DynamicRelaxation.Solve(structure, loads, deltaFreeLength, new CoreConfigDR());
            double drElongationMm = (drResult.Nodes.Coordinates[1, 0] - 3.0) * 1000;
            Console.WriteLine($"Plain DR elongation = {drElongationMm}mm, tension={drResult.Elements.Tension[0]}N");

            // SRDR with an EXPLICIT imperfection array in meters - exactly what
            // MuscleApp.Solvers.SRDR.SRDR.Solve() builds from a GH "Imperfection Ratio" input
            // of 0.00333: imperfection[i] = ratio * FreeLength (both in meters here).
            double[] explicitImperfection = new double[] { 0.00333 * 3.0 };
            var srdrResult = StiffnessReductionDynamicRelaxation.Solve(
                structure, loads, deltaFreeLength, drConfig, srdrConfig,
                new double[] { strutInertia }, explicitImperfection);

            double srdrElongationMm = (srdrResult.Nodes.Coordinates[1, 0] - 3.0) * 1000;
            Console.WriteLine($"SRDR elongation = {srdrElongationMm}mm, tension={srdrResult.Elements.Tension[0]}N, " +
                $"outerIter={srdrConfig.NOuterIterations}, converged={srdrConfig.Converged}, " +
                $"moment={srdrResult.Elements.Moment[0]}N.m, deflection={srdrResult.Elements.Deflection[0] * 1000}mm");

            Assert.IsNotNull(srdrResult);
            Assert.IsTrue(srdrConfig.Converged);
            Assert.AreEqual(-0.964, drElongationMm, 0.01, "Plain DR elongation");
            Assert.AreEqual(-1.771, srdrElongationMm, 0.01, "SRDR elongation (hand calc: Kred/K=0.544 at N/Ncr=0.696)");
            Assert.AreEqual(269.7, srdrResult.Elements.Moment[0], 1.0, "Moment M = N*e_max (hand calc)");
            Assert.AreEqual(32.9, srdrResult.Elements.Deflection[0] * 1000, 0.1, "Amplified deflection e_max (hand calc)");
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_SRDR_DefaultConfig_IncreasingLoad_TimingAndConvergence()
        {
            // Sanity check across a small range of load magnitudes, using the SAME default
            // CoreConfigDR()/CoreConfigSRDR() the Grasshopper component falls back to when
            // those inputs are left unconnected. [Timeout] fails the test instead of hanging
            // this process if a future regression makes it stop converging quickly.
            double[] scales = { 1.0, 3.0, 8.0 };

            foreach (double scale in scales)
            {
                var structure = BuildSimplex();
                double[] loads = new double[18];
                loads[2] = 45.7 * scale; loads[5] = 45.7 * scale; loads[8] = 45.7 * scale;
                loads[11] = 41.6 * scale; loads[14] = 41.6 * scale; loads[17] = 41.6 * scale;

                double[] deltaFreeLength = new double[12];
                deltaFreeLength[0] = 0.835e-3; deltaFreeLength[1] = 0.835e-3; deltaFreeLength[2] = 0.835e-3;
                deltaFreeLength[8] = -35e-3;

                var drConfig = new CoreConfigDR(); // production defaults
                var srdrConfig = new CoreConfigSRDR(); // production defaults

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = StiffnessReductionDynamicRelaxation.Solve(
                    structure, loads, deltaFreeLength, drConfig, srdrConfig, Inertia(), null);
                sw.Stop();

                Console.WriteLine($"scale={scale}: {sw.ElapsedMilliseconds}ms, outerIter={srdrConfig.NOuterIterations}, " +
                    $"converged={srdrConfig.Converged}, tension0={(result != null ? result.Elements.Tension[0].ToString() : "null")}");

                Assert.IsNotNull(result, $"Solve returned null at scale={scale}");
                Assert.IsFalse(double.IsNaN(result.Elements.Tension[0]), $"NaN tension at scale={scale}");
            }
        }

        [TestMethod]
        public void Test_SRDR_HeavyLoad_NearBuckling_DoesNotHangOrCrash()
        {
            var structure = BuildSimplex();

            // Scale the load way up to push a strut's compression force towards/near its
            // Euler critical load - the scenario reported to freeze Rhino.
            double scale = 200.0;
            double[] loads = new double[18];
            loads[2] = 45.7 * scale; loads[5] = 45.7 * scale; loads[8] = 45.7 * scale;
            loads[11] = 41.6 * scale; loads[14] = 41.6 * scale; loads[17] = 41.6 * scale;

            double[] deltaFreeLength = new double[12];
            deltaFreeLength[0] = 0.835e-3; deltaFreeLength[1] = 0.835e-3; deltaFreeLength[2] = 0.835e-3;
            deltaFreeLength[8] = -35e-3;

            var drConfig = new CoreConfigDR(dt: 0.01, massAmplFactor: 1.0, minMass: 0.005,
                maxTimeStep: 1000, maxKEResets: 100, zeroResidualRTol: 1e-4, zeroResidualATol: 1e-6);
            var srdrConfig = new CoreConfigSRDR(forceTol: 1.0, maxOuterIterations: 100);

            var srdrResult = StiffnessReductionDynamicRelaxation.Solve(
                structure, loads, deltaFreeLength, drConfig, srdrConfig, Inertia(), null);

            Assert.IsNotNull(srdrResult);
            Assert.IsFalse(double.IsNaN(srdrResult.Elements.Tension[0]), "Tension must not be NaN");
        }
    }
}
