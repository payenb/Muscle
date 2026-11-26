# Muscle

# Copyright <2015-2025> <Université catholique de Louvain (UCLouvain)>

# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at

#     http://www.apache.org/licenses/LICENSE-2.0

# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# List of the contributors to the development of Muscle: see NOTICE file.
# Description and complete License: see NOTICE file.
# This fork (Muscle 2.0): modified by Basile Payen (UCLouvain) with Claude (Anthropic).

"""The three places that state the musclepy version must agree:
musclepy.__version__, pyproject.toml, and MusclePyVersion.Expected in MuscleCore (the version
the Grasshopper plugin expects, checked when Python starts)."""

import re
import unittest
from pathlib import Path

import musclepy

REPO = Path(__file__).resolve().parents[2]


class TestVersion(unittest.TestCase):

    def test_pyproject_matches_package(self):
        text = (REPO / "pyproject.toml").read_text(encoding="utf-8")
        version = re.search(r'^version\s*=\s*"([^"]+)"', text, re.MULTILINE).group(1)
        self.assertEqual(version, musclepy.__version__)

    def test_plugin_expects_this_package(self):
        text = (REPO / "src" / "MuscleCore" / "PythonNETInit" / "MusclePyVersion.cs").read_text(encoding="utf-8")
        expected = re.search(r'public const string Expected\s*=\s*"([^"]+)"', text).group(1)
        self.assertEqual(expected, musclepy.__version__,
                         "MusclePyVersion.Expected (C#) and musclepy.__version__ differ: update both together")

    def test_tested_package_is_the_repository_one(self):
        self.assertTrue(Path(musclepy.__file__).resolve().is_relative_to(REPO / "src" / "MusclePy"),
                        f"tests run against {musclepy.__file__}, not the repository's musclepy")


if __name__ == '__main__':
    unittest.main()
