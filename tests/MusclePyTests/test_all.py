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

import unittest
import os
import sys

# Add MusclePy source directory to Python path
src_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../src/MusclePy'))
sys.path.append(src_dir)

# Add tests directory to Python path
tests_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
sys.path.append(tests_dir)

# Import all test modules
from MusclePyTests.femodel.test_pyelements import TestPyElements
from MusclePyTests.femodel.test_pynodes import TestPyNodes
from MusclePyTests.femodel.test_prestress import TestFreeLengthFromTension, TestStructureWithTension
from MusclePyTests.solvers.linear_dm.test_linear_dm_2bars_truss import TestLinearDM_2BarsTruss
from MusclePyTests.solvers.linear_dm.test_linear_dm_3prestressedcables import TestLinearDM_3PrestressedCables
from MusclePyTests.solvers.linear_dm.test_linear_dm_perturbation import TestLinearDM_Perturbation
from MusclePyTests.solvers.nonlinear_dm.test_nonlinear_dm_2bars_truss import TestNonlinearDM_2BarsTruss
from MusclePyTests.solvers.nonlinear_dm.test_nonlinear_dm_loose_mechanism import TestNonlinearDM_LooseMechanism
from MusclePyTests.solvers.nonlinear_dm.test_nonlinear_dm_prestressed_tight_rope import TestNonlinearDM_PrestressedTightRope
from MusclePyTests.solvers.svd.test_svd_2cables import TestSVD2Cables
from MusclePyTests.solvers.svd.test_svd_3cables import TestSVD3Cables
from MusclePyTests.solvers.svd.test_svd_simplex import TestSVDSimplex
from MusclePyTests.solvers.svd.test_self_stress import TestSelfStressModes
# Import Dynamic Relaxation test modules
from MusclePyTests.solvers.dr.test_dr_2cables import TestDR_2Cables
from MusclePyTests.solvers.dr.test_dr_3simplecables import TestDR_3SimpleCables
from MusclePyTests.solvers.dr.test_dr_3complexcables import TestDR_3ComplexCables
from MusclePyTests.solvers.dr.test_dr_simplex import TestDR_Simplex
# Import Dynamic Modal Analysis test module
from MusclePyTests.solvers.dynamic.test_dynamic_simplex import TestDynamicSimplex
from MusclePyTests.solvers.dynamic.test_dynamic_masses import TestDynamicMasses
# Import Stiffness-Reduction Dynamic Relaxation test modules
from MusclePyTests.solvers.srdr.test_formulas_srdr import TestFormulasSRDR
from MusclePyTests.solvers.srdr.test_srdr_simplex import TestSRDR_Simplex
from MusclePyTests.solvers.stability.test_tangent_stiffness_stability import TestTangentStiffness, TestTangentStiffnessStability, TestModalAnalysisOfUnstableStructure
from MusclePyTests.solvers.stability.test_prestress_stability import TestPrestressStability
from MusclePyTests.validation.test_feron2023_simplex import TestFeron2023Simplex


def create_test_suite():
    """Create a test suite containing all tests."""
    # Create a test suite
    suite = unittest.TestSuite()
    
    # Add test classes
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestPyElements))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestPyNodes))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestFreeLengthFromTension))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestStructureWithTension))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestLinearDM_2BarsTruss))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestLinearDM_3PrestressedCables))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestLinearDM_Perturbation))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestNonlinearDM_2BarsTruss))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestNonlinearDM_LooseMechanism))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestNonlinearDM_PrestressedTightRope))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestSVD2Cables))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestSVD3Cables))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestSVDSimplex))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestSelfStressModes))
    # Add Dynamic Relaxation test classes
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestDR_2Cables))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestDR_3SimpleCables))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestDR_3ComplexCables))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestDR_Simplex))
    # Add Dynamic Modal Analysis test classes
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestDynamicSimplex))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestDynamicMasses))
    # Add Stiffness-Reduction Dynamic Relaxation test classes
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestFormulasSRDR))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestSRDR_Simplex))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestTangentStiffness))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestTangentStiffnessStability))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestModalAnalysisOfUnstableStructure))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestPrestressStability))
    suite.addTests(unittest.TestLoader().loadTestsFromTestCase(TestFeron2023Simplex))


    return suite

if __name__ == '__main__':
    # Create and run the test suite
    suite = create_test_suite()
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Exit with non-zero code if tests failed
    sys.exit(not result.wasSuccessful())
