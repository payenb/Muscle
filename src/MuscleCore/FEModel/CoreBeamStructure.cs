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

namespace MuscleCore.FEModel
{
    /// <summary>
    /// BEAM FE MODEL ONLY - not used by the axial DR/SRDR pipeline (see CoreTruss).
    ///
    /// Pure data container combining CoreBeamNodes and CoreBeamElements, mirroring CoreTruss.
    /// </summary>
    public class CoreBeamStructure
    {
        public CoreBeamNodes Nodes { get; set; }
        public CoreBeamElements Elements { get; set; }
        public bool IsInEquilibrium { get; set; }

        public CoreBeamStructure(CoreBeamNodes nodes, CoreBeamElements elements)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            Elements = elements ?? throw new ArgumentNullException(nameof(elements));

            if (Elements.Nodes != nodes)
            {
                throw new ArgumentException("Elements must reference the same nodes instance", nameof(elements));
            }
        }

        public CoreBeamStructure(CoreBeamNodes nodes, CoreBeamElements elements, bool isInEquilibrium)
            : this(nodes, elements)
        {
            IsInEquilibrium = isInEquilibrium;
        }
    }
}
