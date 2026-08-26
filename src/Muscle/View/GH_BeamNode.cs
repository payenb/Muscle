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
using Rhino.Geometry;
using MuscleApp.ViewModel;

namespace Muscle.View
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial Element/Truss/DR/SRDR pipeline (see
    /// GH_Node). Grasshopper wrapper for MuscleApp.ViewModel.BeamNode.
    /// </summary>
    public class GH_BeamNode : GH_Goo<BeamNode>
    {
        public override string TypeDescription { get { return "A Beam Node is a point3d with 6 DOF (3 translations + 3 rotations)."; } }
        public override string TypeName { get { return Value.TypeName; } }
        public override bool IsValid { get { return Value.IsValid; } }

        public override BeamNode Value
        {
            get { return base.Value; }
            set { base.Value = value; }
        }

        public GH_BeamNode()
        {
            Value = new BeamNode();
        }

        public GH_BeamNode(BeamNode n)
        {
            Value = n;
        }

        public GH_BeamNode(GH_Goo<BeamNode> gh_n)
        {
            Value = gh_n.Value;
        }

        public override bool CastFrom(object source)
        {
            if (source == null) { return false; }
            if (source is GH_Point gh_point)
            {
                Value.Coordinates = gh_point.Value;
                return true;
            }
            if (source is BeamNode n)
            {
                Value = n;
                return true;
            }
            if (source is GH_ObjectWrapper wrapper && wrapper.Value is BeamNode wrapped)
            {
                Value = wrapped;
                return true;
            }
            return base.CastFrom(source);
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Point3d)))
            {
                target = (Q)(object)Value.Coordinates;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
            {
                target = (Q)(object)new GH_Point(Value.Coordinates);
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(BeamNode)))
            {
                target = (Q)(object)Value;
                return true;
            }
            target = default;
            return false;
        }

        public override IGH_Goo Duplicate()
        {
            return new GH_BeamNode(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
