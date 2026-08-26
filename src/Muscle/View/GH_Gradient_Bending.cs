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

using System.Drawing;
using Grasshopper.GUI.Gradient;

namespace Muscle.View
{
    /// <summary>
    /// Single-sided gradient (0 = white, 1 = orange) for magnitude-only quantities such as
    /// bending moment and deflection, which - unlike axial force (see GH_Gradient_Forces) -
    /// have no tension/compression sign to distinguish.
    /// </summary>
    public class GH_Gradient_Bending
    {
        #region Properties
        public GH_Gradient gradient;
        #endregion Properties

        #region Constructors
        public GH_Gradient_Bending()
        {
            gradient = new GH_Gradient();
            gradient.Linear = true;
            gradient.AddGrip(0, Color.White);
            gradient.AddGrip(1, Color.DarkOrange);
        }

        #endregion Constructors
    }
}
