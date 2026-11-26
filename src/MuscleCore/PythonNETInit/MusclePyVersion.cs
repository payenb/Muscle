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

namespace MuscleCore.PythonNETInit
{
    /// <summary>
    /// Version of musclepy that this build of Muscle expects.
    ///
    /// The C# side and musclepy exchange arrays whose meaning (units, argument names) must agree:
    /// for instance, since musclepy 2.0.1 the inertia sent to the SRDR solver is in m^4 (it was in
    /// mm^4 before). An older musclepy then gives wrong SRDR results without any error. The version
    /// is therefore checked when Python starts, and a warning is shown on mismatch.
    ///
    /// Keep Expected equal to musclepy.__version__ and to the version in pyproject.toml:
    /// tests/MusclePyTests/test_version.py checks it. Increment all three whenever the exchange
    /// between C# and Python changes.
    /// </summary>
    public static class MusclePyVersion
    {
        public const string Expected = "2.0.1";

        /// <summary>
        /// Warning to show when the loaded musclepy is not the expected one, or null if it is.
        /// </summary>
        /// <param name="loadedVersion">musclepy.__version__ of the loaded package (null if unknown)</param>
        /// <param name="loadedPath">musclepy.__file__ of the loaded package (null if unknown)</param>
        /// <param name="condaEnvName">conda environment Python was started from (null if unknown)</param>
        public static string? Mismatch(string? loadedVersion, string? loadedPath = null, string? condaEnvName = null)
        {
            if (loadedVersion == Expected)
                return null;

            string loaded = string.IsNullOrEmpty(loadedVersion) ? "an unknown version of musclepy" : $"musclepy {loadedVersion}";
            string from = string.IsNullOrEmpty(loadedPath) ? "" : $" (from {loadedPath})";
            string env = string.IsNullOrEmpty(condaEnvName) ? "the conda environment" : $"the '{condaEnvName}' conda environment";
            return $"Version mismatch: {loaded} is loaded{from}, but this Muscle2.gha expects musclepy {Expected}. "
                 + "Results may be wrong (for instance SRDR, whose inertia units changed in 2.0.1). "
                 + $"Install the matching musclepy in {env}, or install the Muscle2.gha that matches the loaded musclepy.";
        }
    }
}
