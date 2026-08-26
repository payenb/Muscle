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
# This fork (Muscle 2.0): added by Basile Payen (UCLouvain) with Claude (Anthropic).


class PyConfigBeamDR:
    """
    BEAM FE MODEL ONLY. Configuration for the explicit kinetic-damping Dynamic Relaxation
    Beam solver (musclepy.solvers.beam_dr) - the DR-specific parameters (dt, mass_ampl_factor,
    min_mass, max_time_step, max_ke_reset) mirror musclepy.solvers.dr.PyConfigDR exactly,
    generalized to 6 DOF/node. Convergence itself uses the same simple absolute residual-norm
    threshold as musclepy.solvers.beam_fem.PyConfigBeam (tol_force), for a consistent output
    shape between the two Beam solvers.
    """

    def __init__(self, dt=0.01, mass_ampl_factor=1.0, min_mass=0.005, max_time_step=10000,
                 max_ke_reset=1000, tol_force=1e-5):
        """
        Args:
            dt: [s] - time step for the explicit time-incremental method
            mass_ampl_factor: amplification factor for the fictitious masses
            min_mass: minimum fictitious mass/inertia applied to each DOF if near-zero
                stiffness is detected (e.g. an isolated hinge-released rotation)
            max_time_step: maximum number of time steps before giving up
            max_ke_reset: maximum number of kinetic energy resets before giving up
            tol_force: [N or N.m] - residual norm below which equilibrium is considered reached
        """
        self.dt = dt if dt > 0 else 0.01
        self.mass_ampl_factor = mass_ampl_factor if mass_ampl_factor > 0 else 1.0
        self.min_mass = min_mass if min_mass > 0 else 0.005
        self.huge_mass = 1e15  # mass applied to fixed DOF
        self.max_time_step = max_time_step if max_time_step > 0 else 10000
        self.max_ke_reset = max_ke_reset if max_ke_reset > 0 else 1000
        self.tol_force = tol_force if tol_force > 0 else 1e-5

        # Returned to the user for information regarding the solver's performance.
        self.converged = False
        self.n_time_step = 0
        self.n_ke_reset = 0
        self.residual = None
