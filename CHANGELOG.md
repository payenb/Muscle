# Changelog

All notable changes to musclepy project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - Muscle 2.0 (branch `packaging`)

musclepy version 2.0.1 (was 2.0.0 before the SI units change, so that the plugin can tell the two
apart).

### Fixed
- **Rhino closed when a solver raised a Python error.** The C# solvers released the GIL
  (`BeginAllowThreads`) but restored it only on success; after a Python exception the next Python call
  ran without a thread state and aborted the process ("Fatal Python error: PyEval_SaveThread: NULL
  tstate"). All ten MuscleCore solvers now restore it in a `finally` block. Seen with the modal
  analysis when a free node had no mass.
- **Modal analysis: a free DOF without mass** now raises a clear error naming the nodes and
  directions (supports are often free in some directions), instead of a cryptic eigensolver message.
- **Modal analysis in Grasshopper: element masses** are computed from the elements (free length ×
  area × specific mass) when the "Element Mass" input is empty, as its description said. They were
  zero, so the lumped and consistent options silently ignored the element masses. Existing files
  with an empty input and option 1 or 2 now include the element masses (lower frequencies).
- **Linear and nonlinear displacement methods crashed with numpy >= 2.5** (`float()` of a
  1-element array in `linear_dm._post_process`). DeprecationWarnings raised inside musclepy now
  fail the tests.
- **Exact tangent stiffness** `EA/L0·eeᵀ + (t/L)(I − eeᵀ)` (Guest 2006) in
  `utils.matrix_calculations.compute_tangent_stiffness_matrix`, used by the linear and nonlinear
  displacement methods, the modal analysis and the new stability test. The former geometric
  stiffness `(t/L)·I` added `t/L` to the axial stiffness of every element, an error of the order of
  the strain `t/EA`. With the linear displacement method, the tension increment is now
  `EA/L0·Δl` instead of `(EA/L0 + t/L)·Δl`.
  The fictitious masses of the dynamic relaxation deliberately keep Barnes' rule `EA/L0 + t/L`
  (Bel Hadj Ali et al. 2011, eq. 19-20), a conservative bound for the stability of the explicit
  integration (`compute_local_dr_mass_geometric_stiffness_matrices`). DR and SRDR results are
  therefore unchanged: on the 61 DR/SRDR calls of the test suite, results are bit-identical and the
  numbers of time steps, kinetic energy resets and outer SRDR iterations are the same as before.
- **Modal analysis no longer hides unstable modes.** It computed `sqrt(|ω²|)`, so a negative
  eigenvalue appeared as a positive frequency. Such a mode now gets a negative frequency
  `−sqrt(|ω²|)/2π`, with a `RuntimeWarning` (and a warning on the Grasshopper component).
  `PyResultsDynamic` gains `eigenvalues` (ω²) and `unstable_mode_count`.
- **Reproducible displacement methods on singular structures**: the random perturbation of
  `linear_dm.perturb` is seeded (`PERTURBATION_SEED`).
- **Beam FEM / Beam DR input labels** read `F (kN)` and `M (kN.m)` (they said `N` and `N.m`, but
  take Point Loads entered in kN). Existing .gh files keep their connections.

### Changed
- **Units: musclepy is SI only** (m, m², m⁴, N, N·m, Pa, kg, Hz) and never converts units.
  `main_stiffness_reduction_dr` now takes the inertia in m⁴ (it took mm⁴) and `srdr.formulas` works
  in SI (it worked in mm, mm², mm⁴, MPa). What Grasshopper shows is unchanged: the conversions to kN,
  MPa, mm... are done by the Grasshopper components only.

### Added
- **Version check at startup**: StartPython.NET shows which musclepy is loaded (version and path)
  and warns when it is not the version this Muscle2.gha expects (`MusclePyVersion.Expected` in
  MuscleCore). A stale plugin or package otherwise gives wrong results silently: an old Muscle2.gha
  (inertia sent in mm⁴) with musclepy 2.0.1 (inertia in m⁴) made SRDR stop after one outer
  iteration with the nominal stiffness. `tests/MusclePyTests/test_version.py` keeps the package,
  `pyproject.toml` and the C# constant in step.
- `tools/install-grasshopper.ps1`: builds and copies Muscle2.gha, MuscleApp.dll and MuscleCore.dll
  to `%APPDATA%\Grasshopper\Libraries\Muscle 2.0`; refuses while Rhino is open.
- `main_tangent_stiffness_stability` and `PyResultsStability` (`musclepy.solvers.stability`):
  stable / critical / unstable from the smallest eigenvalue of the tangent stiffness, with its mode.
- `main_prestress_stability` (`musclepy.solvers.stability`): Pellegrino-Calladine test. The
  geometric stiffness `(t/L)(I − eeᵀ)` of a self-stress state is projected on the infinitesimal
  mechanisms of the SVD; positive definite = prestress stable. Needs no material properties.
- `free_length_from_tension` and `structure_with_tension` (`musclepy.femodel.prestress`): free
  lengths `L0 = L / (1 + t0/EA)` giving the wanted axial forces `t0` in the current geometry, and a
  consistently prestressed copy of a structure. Giving a `tension` without the matching free lengths
  is not enough: the dynamic relaxation recomputes the forces from the free lengths and the
  prestress disappears.
- Validation tests against the published values of the experimental simplex of Feron,
  Rhode-Barbarigos and Latteur (2023, J. Struct. Eng. 149(7), 04023073), data taken from the
  repository tensegrite-vs-treillis (`tests/MusclePyTests/validation`), and the regression case
  `feron2023_simplex_test_1`. Muscle reproduces table 1 (lengths, EA/L), table 2 (self-stress mode
  0.393 / 0.681, sensitivities 602 / 237 / 410 N/mm), table 3 (Δa = 1001.5 N for the 1 kN scenario),
  a_gov = 8.81 kN and the dynamic relaxation results of test 1. Gaps with the printed values: H at the
  start of test 1 is 1945.5 mm instead of 1946.0 mm; at 2.1 kN per top node the struts carry
  −10.87 kN instead of −10.80 kN (0.7 %).

### Effect of the exact tangent stiffness on results
Regression references updated: `simplex_dynamic_lumped_mass`, `simplex_dynamic_consistent_mass`,
`two_bars_nonlinear_dm`, `tight_rope_nonlinear_dm_3_stages`. All other references (DR, SRDR, SVD,
linear DM, beams) are unchanged.

Natural frequencies of the experimental simplex (Feron et al. 2023) [Hz]:

| Mode | Lumped, before | Lumped, after | Consistent, before | Consistent, after | Relative change (lumped / consistent) |
|---|---|---|---|---|---|
| 1 | 2.16650 | 2.16647 | 2.34557 | 2.34553 | −1.5e-5 / −1.5e-5 |
| 2 | 34.43843 | 34.43534 | 36.29527 | 36.29203 | −9.0e-5 / −8.9e-5 |
| 3 | 41.76735 | 41.76331 | 44.48040 | 44.47613 | −9.7e-5 / −9.6e-5 |
| 4 | 59.94175 | 59.93682 | 63.70670 | 63.70143 | −8.2e-5 / −8.3e-5 |
| 5 | 94.05128 | 94.04157 | 101.75550 | 101.74516 | −1.0e-4 / −1.0e-4 |
| 6 | 105.14204 | 105.13223 | 114.78970 | 114.77948 | −9.3e-5 / −8.9e-5 |
| 7 | 110.94152 | 110.93136 | 118.77303 | 118.76175 | −9.2e-5 / −9.5e-5 |
| 8 | 115.49223 | 115.48093 | 122.56863 | 122.55635 | −9.8e-5 / −1.0e-4 |
| 9 | 139.77826 | 139.76708 | 148.28875 | 148.27689 | −8.0e-5 / −8.0e-5 |
| 10 | 233.81883 | 233.81855 | 252.78008 | 252.77951 | −1.2e-6 / −2.3e-6 |
| 11 | 259.92856 | 259.93062 | 288.28877 | 288.29089 | +7.9e-6 / +7.3e-6 |
| 12 | 289.03720 | 289.03876 | 324.22020 | 324.22223 | +5.4e-6 / +6.3e-6 |

Mode shapes change by at most 1e-4.

Nonlinear displacement method, relative error against the exact equilibrium (deflection / tension
of the loaded node). The method is incremental, without equilibrium iterations, so it drifts by
O(1/n_steps). With the exact tangent the error tends to 0; with the former one it tends to a
wrong solution (about 1e-3). At 100 steps, on the tight rope, the two errors partly cancelled:

| Case | n_steps | Former tangent | Exact tangent |
|---|---|---|---|
| Tight rope, stage 1 (5.109 kN, prestress 20 kN) | 100 | +3.4e-3 / +3.4e-4 | +4.6e-3 / −8.7e-4 |
| | 1 000 | −7.6e-4 / +1.1e-3 | +4.6e-4 / −8.5e-5 |
| | 10 000 | −1.2e-3 / +1.2e-3 | +4.6e-5 / −8.5e-6 |
| Two-bar truss (100 kN) | 100 | +1.4e-3 / −2.5e-5 | −4.3e-5 / −2.9e-5 |
| | 1 000 | +1.4e-3 / +1.2e-6 | −4.3e-6 / −2.9e-6 |
| | 10 000 | +1.4e-3 / +3.8e-6 | −4.3e-7 / −2.9e-7 |

Values at 100 steps (regression cases):

| Case | Quantity | Exact | Before | After |
|---|---|---|---|---|
| Two-bar truss | deflection [mm] | 5.68113 | 5.68893 | 5.68088 |
| | tension [N] | −70912.97 | −70911.22 | −70910.93 |
| Tight rope, stage 1 | deflection [mm] | 75.06887 | 75.32580 | 75.41715 |
| | tension [N] | 34124.50 | 34136.10 | 34094.78 |
| Tight rope, stage 2 (9.988 kN) | deflection [mm] | 105.23279 | 105.36406 | 105.55460 |
| | tension [N] | 47718.74 | 47789.65 | 47704.19 |

## [1.1.0] - 2025-07-21

### Added
- **Dynamic Modal Analysis**: New `musclepy.solvers.dynamic` module for computing natural frequencies and mode shapes
  - `main_dynamic_modal_analysis()` function for modal analysis of structures
  - `PyResultsDynamic` class to store modal analysis results (frequencies, mode shapes, mass matrix)
  - Computation of tangent stiffness matrix including geometric stiffness effects
  - Mass matrix computation with lumped and consistent mass options

### Dependencies
- numpy>=1.26.4
- scipy>=1.13.1
- Python>=3.9

## [1.0.0] - Previous Release

### Added
- Initial release of MusclePy
- Basic finite element modeling capabilities
- SVD analysis for tensegrity structures
- Linear and nonlinear displacement methods
- Dynamic relaxation solver
- Self-stress mode analysis
