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
    /// <summary>BEAM FE MODEL ONLY - see CoreBeamElements.</summary>
    public class PyBeamElementsDecoder : IPyObjectDecoder
    {
        public bool CanDecode(PyType objectType, Type targetType)
        {
            if (targetType != typeof(CoreBeamElements))
                return false;

            using (Py.GIL())
            {
                try
                {
                    return objectType.Name == "PyBeamElements";
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
            if (typeof(T) != typeof(CoreBeamElements))
                return false;

            using (Py.GIL())
            {
                try
                {
                    dynamic py = pyObj.As<dynamic>();

                    var nodes = py.nodes.As<CoreBeamNodes>();

                    var endNodes = AsInt2dArray(py.end_nodes);
                    var area = py.area.tolist().As<double[]>();
                    var youngs = py.youngs.tolist().As<double[]>();
                    var inertia = py.inertia.tolist().As<double[]>();
                    var torsionConstant = py.torsion_constant.tolist().As<double[]>();
                    var shearModulus = py.shear_modulus.tolist().As<double[]>();
                    var freeLength = py.free_length.tolist().As<double[]>();
                    var hingeStart = py.hinge_start.tolist().As<bool[]>();
                    var hingeEnd = py.hinge_end.tolist().As<bool[]>();
                    var orientation = As2dArray(py.orientation);

                    var tension = py.tension.tolist().As<double[]>();
                    var momentStart = As2dArray(py.moment_start);
                    var momentEnd = As2dArray(py.moment_end);
                    var torsion = py.torsion.tolist().As<double[]>();

                    var elements = new CoreBeamElements(
                        nodes: nodes,
                        endNodes: endNodes,
                        area: area,
                        youngs: youngs,
                        inertia: inertia,
                        torsionConstant: torsionConstant,
                        shearModulus: shearModulus,
                        freeLength: freeLength,
                        hingeStart: hingeStart,
                        hingeEnd: hingeEnd,
                        orientation: orientation,
                        tension: tension,
                        momentStart: momentStart,
                        momentEnd: momentEnd,
                        torsion: torsion
                    );

                    value = (T)(object)elements;
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
