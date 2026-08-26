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

using MuscleCore.FEModel;
using Python.Runtime;
using static MuscleCore.Converters.DecoderHelper;

namespace MuscleCore.Converters
{
    /// <summary>BEAM FE MODEL ONLY - see CoreBeamNodes.</summary>
    public class PyBeamNodesDecoder : IPyObjectDecoder
    {
        public bool CanDecode(PyType objectType, Type targetType)
        {
            if (targetType != typeof(CoreBeamNodes))
                return false;

            using (Py.GIL())
            {
                try
                {
                    return objectType.Name == "PyBeamNodes";
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool TryDecode<T>(PyObject pyObj, out T? value)
        {
            value = default;
            if (typeof(T) != typeof(CoreBeamNodes))
                return false;

            using (Py.GIL())
            {
                try
                {
                    dynamic py = pyObj.As<dynamic>();

                    var initialCoords = As2dArray(py.initial_coordinates);
                    var coordinates = As2dArray(py.coordinates);
                    var dofs = AsBool2dArray(py.dof);
                    var loads = As2dArray(py.loads);
                    var displacements = As2dArray(py.displacements);
                    var reactions = As2dArray(py.reactions);
                    var resistingForces = As2dArray(py.resisting_forces);
                    var residuals = As2dArray(py.residuals);

                    var nodes = new CoreBeamNodes(
                        initialCoordinates: initialCoords,
                        coordinates: coordinates,
                        dof: dofs,
                        count: (int)py.count,
                        loads: loads,
                        displacements: displacements,
                        reactions: reactions,
                        resistingForces: resistingForces,
                        residuals: residuals
                    );

                    value = (T)(object)nodes;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
