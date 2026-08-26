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

namespace MuscleCore.Converters
{
    /// <summary>BEAM FE MODEL ONLY - see CoreBeamElements.</summary>
    public class PyBeamElementsEncoder : IPyObjectEncoder
    {
        public bool CanEncode(Type type)
        {
            return type == typeof(CoreBeamElements);
        }

        public PyObject? TryEncode(object obj)
        {
            if (!CanEncode(obj.GetType()))
                return null;

            var elements = (CoreBeamElements)obj;
            using (Py.GIL())
            {
                dynamic musclepy = Py.Import("musclepy");

                return musclepy.solvers.beam_fem.PyBeamElements(
                    nodes: elements.Nodes.ToPython(),
                    end_nodes: elements.EndNodes,
                    area: elements.Area,
                    youngs: elements.Youngs,
                    inertia: elements.Inertia,
                    torsion_constant: elements.TorsionConstant,
                    shear_modulus: elements.ShearModulus,
                    free_length: elements.FreeLength,
                    hinge_start: elements.HingeStart,
                    hinge_end: elements.HingeEnd,
                    orientation: elements.Orientation
                );
            }
        }
    }
}
