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

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace Muscle.View
{
    /// <summary>
    /// Loads component icons (24x24 PNGs embedded under Resources/Icons/, see
    /// tools/IconGenerator) as Bitmaps, one per icon name, cached after first use.
    /// </summary>
    public static class IconLoader
    {
        private static readonly Dictionary<string, Bitmap> _cache = new Dictionary<string, Bitmap>();

        /// <summary>
        /// Returns the icon Bitmap for the given name (matching a file under
        /// Resources/Icons/&lt;name&gt;.png), or null if not found - a component whose icon
        /// fails to load falls back to Grasshopper's default nickname box, not a crash.
        /// </summary>
        public static Bitmap Get(string name)
        {
            Bitmap cached;
            if (_cache.TryGetValue(name, out cached))
                return cached;

            Bitmap bitmap = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"Muscle.Resources.Icons.{name}.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                        bitmap = new Bitmap(stream);
                }
            }
            catch
            {
                bitmap = null;
            }

            _cache[name] = bitmap;
            return bitmap;
        }
    }
}
