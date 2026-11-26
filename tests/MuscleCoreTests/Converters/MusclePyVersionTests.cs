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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuscleCore.PythonNETInit;

namespace MuscleCoreTests.Converters
{
    [TestClass]
    public class MusclePyVersionTests
    {
        [TestMethod]
        public void Mismatch_IsNull_ForTheExpectedVersion()
        {
            Assert.IsNull(MusclePyVersion.Mismatch(MusclePyVersion.Expected, @"C:\x\musclepy\__init__.py", "muscle-dev"));
        }

        [TestMethod]
        public void Mismatch_NamesBothVersionsThePathAndTheEnvironment()
        {
            string warning = MusclePyVersion.Mismatch("2.0.0", @"C:\envs\muscle\Lib\site-packages\musclepy\__init__.py", "muscle");
            Assert.IsNotNull(warning);
            StringAssert.Contains(warning, "musclepy 2.0.0");
            StringAssert.Contains(warning, $"expects musclepy {MusclePyVersion.Expected}");
            StringAssert.Contains(warning, @"C:\envs\muscle\Lib\site-packages\musclepy\__init__.py");
            StringAssert.Contains(warning, "'muscle'");
        }

        [TestMethod]
        public void Mismatch_ReportsAnUnknownVersion()
        {
            string warning = MusclePyVersion.Mismatch(null);
            Assert.IsNotNull(warning);
            StringAssert.Contains(warning, "unknown version");
        }

        [TestMethod]
        public void LaunchedPython_LoadsTheExpectedMusclePy()
        {
            // The test configuration loads the repository's musclepy (developer mode, see
            // CoreTestsConfig.cs): it must be the version this build expects, so no warning.
            PythonNETManager.Launch(CoreTestsConfig.testConfig);
            try
            {
                Assert.IsTrue(PythonNETManager.IsInitialized);
                Assert.AreEqual(MusclePyVersion.Expected, PythonNETManager.LoadedMusclePyVersion);
                Assert.IsNotNull(PythonNETManager.LoadedMusclePyPath);
                StringAssert.Contains(PythonNETManager.LoadedMusclePyPath, "musclepy");
                Assert.IsNull(PythonNETManager.VersionWarning);
            }
            finally
            {
                PythonNETManager.ShutDown();
            }
            Assert.IsNull(PythonNETManager.LoadedMusclePyVersion);
            Assert.IsNull(PythonNETManager.VersionWarning);
        }
    }
}
