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
// ------------------------------------------------------------------------------------------------------------

using MuscleCore.Solvers;
using Python.Runtime;

namespace MuscleCore.Converters
{
    public class PyConfigSRDREncoder : IPyObjectEncoder
    {
        public bool CanEncode(Type type)
        {
            return type == typeof(CoreConfigSRDR);
        }

        public PyObject? TryEncode(object obj)
        {
            if (!CanEncode(obj.GetType()))
                return null;

            var config = (CoreConfigSRDR)obj;
            using (Py.GIL())
            {
                try
                {
                    dynamic musclepy = Py.Import("musclepy");

                    // Create a PyConfigSRDR instance from the module
                    return musclepy.PyConfigSRDR(
                        force_tol: config.ForceTol,
                        max_outer_iterations: config.MaxOuterIterations,
                        warm_start: config.WarmStart
                    );
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
