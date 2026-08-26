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

using Grasshopper.Kernel.Types;
using MuscleApp.ViewModel;

namespace Muscle.View
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial Element/Truss/DR/SRDR pipeline (see
    /// GH_Truss). Grasshopper wrapper for MuscleApp.ViewModel.BeamStructure.
    /// </summary>
    public class GH_BeamStructure : GH_Goo<BeamStructure>
    {
        public override BeamStructure Value
        {
            get { return base.Value; }
            set { base.Value = value; }
        }

        public override bool IsValid { get { return Value.IsValid; } }
        public override string TypeName { get { return Value.TypeName; } }
        public override string TypeDescription { get { return "A Beam Structure is composed of Beam Elements (6-DOF beam-columns) linking Beam Nodes."; } }

        public override string ToString()
        {
            return Value.ToString();
        }

        public GH_BeamStructure()
        {
            Value = new BeamStructure();
        }

        public GH_BeamStructure(BeamStructure structure)
        {
            Value = structure;
        }

        public GH_BeamStructure(GH_Goo<BeamStructure> gh_structure)
        {
            Value = gh_structure.Value;
        }

        public override IGH_Goo Duplicate()
        {
            return new GH_BeamStructure(this);
        }

        public override bool CastFrom(object source)
        {
            if (source is BeamStructure s)
            {
                Value = s;
                return true;
            }
            if (source is GH_ObjectWrapper wrapper && wrapper.Value is BeamStructure wrapped)
            {
                Value = wrapped;
                return true;
            }
            return base.CastFrom(source);
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(BeamStructure)))
            {
                target = (Q)(object)Value;
                return true;
            }
            target = default;
            return false;
        }

        public override object ScriptVariable()
        {
            return Value;
        }
    }
}
