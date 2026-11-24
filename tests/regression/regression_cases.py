"""Non-regression cases for musclepy.

Each case is a function with no argument that runs one structural analysis through the
public musclepy API (the same functions MuscleCore calls from Grasshopper) and returns a
dict with two keys:

- "results": {name: array-like} - the quantities compared against tests/reference/<case>.json
  (axial forces, displacements, coordinates, reactions, frequencies, ...)
- "info":    {name: value}       - recorded for context only (iteration counts, ...), never compared

The inputs are deliberately copied here rather than imported from tests/MusclePyTests, so that
editing those unit tests can never silently shift the regression references.

Register a new case by decorating it with @case.
"""

import numpy as np

import musclepy as mp
from musclepy.femodel.pynodes import PyNodes
from musclepy.femodel.pyelements import PyElements
from musclepy.femodel.pytruss import PyTruss
from musclepy.solvers.dr.py_config_dr import PyConfigDR
from musclepy.solvers.srdr.py_config_srdr import PyConfigSRDR
from musclepy.solvers.beam_fem.py_beam_nodes import PyBeamNodes
from musclepy.solvers.beam_fem.py_beam_elements import PyBeamElements
from musclepy.solvers.beam_fem.py_config_beam import PyConfigBeam
from musclepy.solvers.beam_dr.py_config_beam_dr import PyConfigBeamDR

CASES = {}


def case(func):
    """Register a regression case under its function name."""
    CASES[func.__name__] = func
    return func


# ----------------------------------------------------------------------------------------
# Helpers
# ----------------------------------------------------------------------------------------

def _truss_results(result):
    """Standard outputs of a truss solver (DR, SRDR, DM)."""
    return {
        "tension": result.elements.tension,                    # [N] axial forces
        "coordinates": result.nodes.coordinates,               # [m]
        "displacements": result.nodes.displacements,           # [m]
        "reactions": result.nodes.reactions,                   # [N]
        "resisting_forces": result.nodes.resisting_forces,     # [N]
    }


def _dr_info(config):
    return {"n_time_step": int(config.n_time_step), "n_ke_reset": int(config.n_ke_reset)}


def _orthogonal_projector(basis, n):
    """Projector onto the subspace spanned by `basis`, of size (n, n).

    SVD bases (self-stress modes, mechanisms) are not unique (sign, rotation inside a
    degenerate subspace), but the subspace they span is: its projector is what we compare.
    """
    b = np.atleast_2d(np.asarray(basis, dtype=float))
    if b.size == 0:
        return np.zeros((n, n))
    if b.shape[0] != n:
        b = b.T
    q, _ = np.linalg.qr(b)
    return q @ q.T


def _isolated_mode_shapes(frequencies, mode_shapes, rel_gap=1e-4):
    """Sign-normalised unit mode shapes, kept only for non-repeated frequencies.

    A mode shape is only defined up to its sign, and not at all inside a group of repeated
    frequencies (any rotation of the eigenspace is valid). Shapes of repeated frequencies are
    replaced by NaN rows, which the comparison treats as "not compared".
    """
    f = np.asarray(frequencies, dtype=float)
    shapes = np.array(mode_shapes, dtype=float, copy=True)
    for i in range(len(f)):
        neighbours = np.delete(f, i)
        isolated = neighbours.size == 0 or np.min(np.abs(neighbours - f[i])) > rel_gap * max(abs(f[i]), 1e-12)
        if not isolated:
            shapes[i, :] = np.nan
            continue
        v = shapes[i] / np.linalg.norm(shapes[i])
        k = np.argmax(np.abs(v))
        shapes[i] = v * np.sign(v[k])
    return shapes


# ----------------------------------------------------------------------------------------
# Experimental tensegrity simplex (Feron, Rhode-Barbarigos & Latteur, ASCE JSE 2023)
# Same structure as examples/Tensegrity Simplex/*.gh
# ----------------------------------------------------------------------------------------

_SIMPLEX_COORDINATES_MM = np.array([
    [0.00, -2043.82, 0.00],
    [0.00, 0.00, 0.00],
    [1770.00, -1021.91, 0.00],
    [590.00, -2201.91, 1950.00],
    [-431.91, -431.91, 1950.00],
    [1611.91, -431.91, 1950.00],
])
_SIMPLEX_DOF = np.array([
    [False, True, False],
    [False, False, False],
    [True, True, False],
    [True, True, True],
    [True, True, True],
    [True, True, True],
])
_SIMPLEX_END_NODES = np.array([
    [2, 4], [1, 3], [0, 5],                                          # struts
    [1, 2], [0, 1], [0, 2], [4, 5], [3, 4], [3, 5], [2, 5], [1, 4], [0, 3],  # cables
])
_SIMPLEX_TYPE = np.array([-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1])


def _simplex(youngs_cables=(71750,) * 9):
    """Simplex truss, SI units (data given in mm, mm² and MPa, converted here).
    youngs_cables: cable Young's moduli in MPa."""
    nodes = PyNodes(initial_coordinates=_SIMPLEX_COORDINATES_MM * 1e-3, dof=_SIMPLEX_DOF)
    area = np.empty(12)
    area[0:3] = 364.4
    area[3:12] = 50.3
    area = area * 1e-6  # mm² -> m²
    youngs = np.zeros((12, 2))
    youngs[0:3, 0] = 70390
    youngs[3:12, 1] = youngs_cables
    youngs = youngs * 1e6  # MPa -> Pa
    elements = PyElements(nodes=nodes, type=_SIMPLEX_TYPE, end_nodes=_SIMPLEX_END_NODES,
                          area=area, youngs=youngs)
    return PyTruss(nodes, elements)


def _simplex_static_loading():
    loads = np.zeros((6, 3))
    loads[0:3, 2] = 45.7  # N
    loads[3:6, 2] = 41.6  # N
    delta_free_length = np.zeros(12)
    delta_free_length[0:3] = 0.835e-3  # m, struts
    delta_free_length[8] = -35e-3      # m, shortening of one top cable
    return loads, delta_free_length


def _simplex_dr_config():
    return PyConfigDR(zero_residual_rtol=1e-4, zero_residual_atol=1e-6, max_time_step=1000, max_ke_reset=100)


@case
def simplex_dr_cable_shortening():
    """Simplex, dynamic relaxation: self-weight + shortening of one cable."""
    loads, dl = _simplex_static_loading()
    config = _simplex_dr_config()
    result = mp.main_dynamic_relaxation(structure=_simplex(), loads_increment=loads,
                                        free_length_variation=dl, config=config)
    return {"results": _truss_results(result), "info": _dr_info(config)}


@case
def simplex_srdr_default_imperfection():
    """Simplex, stiffness-reduction DR with the default imperfection L0/300 (Payen et al. 2026)."""
    loads, dl = _simplex_static_loading()
    ro, ri = 30e-3, 28e-3  # m
    inertia = np.zeros(12)
    inertia[0:3] = np.pi / 4 * (ro ** 4 - ri ** 4)  # m^4
    dr_config = _simplex_dr_config()
    srdr_config = PyConfigSRDR(force_tol=1.0, max_outer_iterations=100)
    result = mp.main_stiffness_reduction_dr(structure=_simplex(), loads_increment=loads,
                                            free_length_variation=dl, dr_config=dr_config,
                                            srdr_config=srdr_config, inertia=inertia)
    results = _truss_results(result)
    results["youngs"] = result.elements.youngs          # reduced compression moduli
    results["moment"] = result.elements.moment          # second-order moment in struts
    results["deflection"] = result.elements.deflection  # second-order mid-span deflection
    info = {"n_outer_iterations": int(srdr_config.n_outer_iterations), "converged": bool(srdr_config.converged)}
    return {"results": results, "info": info}


@case
def simplex_svd():
    """Simplex, SVD of the equilibrium matrix (rank, singular values, mode subspaces)."""
    structure = _simplex()
    svd = mp.main_singular_value_decomposition(structure)
    n_dof = 3 * structure.nodes.count
    n_el = structure.elements.count
    return {
        "results": {
            "rank_s_m": np.array([svd.r, svd.s, svd.m], dtype=float),
            "singular_values": svd.Sr,
            "self_stress_projector": _orthogonal_projector(svd.Vs, n_el),
            "mechanism_projector": _orthogonal_projector(svd.Um, n_dof),
        },
        "info": {},
    }


def _simplex_dynamic(element_masses_option):
    # Same simplex, as modelled in the dynamic tests: coordinates rounded to 0.1 mm and
    # 3 cables with a slightly different modulus.
    reference = _simplex(youngs_cables=(71750,) * 6 + (72190,) * 3)
    nodes = PyNodes(initial_coordinates=np.array([
        [0.00, -2043.8, 0.00], [0.00, 0.00, 0.00], [1770.00, -1021.9, 0.00],
        [590.00, -2201.9, 1950.00], [-431.9, -431.9, 1950.00], [1611.9, -431.9, 1950.00],
    ]) / 1000, dof=_SIMPLEX_DOF)
    elements = PyElements(nodes=nodes, type=_SIMPLEX_TYPE, end_nodes=_SIMPLEX_END_NODES,
                          area=reference.elements.area, youngs=reference.elements.youngs)
    structure = PyTruss(nodes, elements)

    delta_free_length = np.zeros(12)
    delta_free_length[0:3] = 0.717e-3  # m, self-stress level ~1.5 kN
    self_weight = np.zeros((6, 3))
    self_weight[0:3, 2] = -45.7
    self_weight[3:6, 2] = -50.3
    point_mass = np.zeros((6, 3))
    point_mass[0:3, :] = 2.642
    point_mass[3:6, :] = 3.06
    element_mass = np.zeros(12)
    element_mass[0:3] = 2.63
    element_mass[3:12] = 0.502

    config = _simplex_dr_config()
    form_found = mp.main_dynamic_relaxation(structure=structure, loads_increment=self_weight,
                                            free_length_variation=delta_free_length, config=config)
    modal = mp.main_dynamic_modal_analysis(structure=form_found, point_masses=point_mass,
                                           element_masses=element_mass,
                                           element_masses_option=element_masses_option, n_modes=0)
    results = _truss_results(form_found)
    results["frequencies"] = modal.frequencies  # [Hz]
    results["masses"] = modal.masses            # [kg]
    results["mode_shapes"] = _isolated_mode_shapes(modal.frequencies, modal.mode_shapes)
    return {"results": results, "info": _dr_info(config)}


@case
def simplex_dynamic_lumped_mass():
    """Simplex, form-finding by DR then modal analysis with lumped element masses."""
    return _simplex_dynamic(element_masses_option=1)


@case
def simplex_dynamic_consistent_mass():
    """Simplex, form-finding by DR then modal analysis with consistent element masses."""
    return _simplex_dynamic(element_masses_option=2)


def _feron2023_exact_nodes():
    """[m] Simplex nodes rebuilt from the dimensions of Feron et al. (2023): equilateral triangles
    of side 2043.8 mm centred on the printed bottom nodes, top turned by 30 deg, H = 1950 mm.
    Same as tests/MusclePyTests/validation/test_feron2023_simplex.py (copied on purpose)."""
    import math
    centre = np.array([(0.0 + 0.0 + 1770.0) / 3, (-2043.8 + 0.0 - 1021.9) / 3])  # centroid of the printed bottom nodes [mm]
    radius = 2043.8 / math.sqrt(3.0)
    angles = np.radians([-120.0, 120.0, 0.0])
    out = []
    for z, turn in ((0.0, 0.0), (1950.0, math.radians(30.0))):
        for a in angles:
            out.append([centre[0] + radius * math.cos(a + turn), centre[1] + radius * math.sin(a + turn), z])
    return np.array(out) * 1e-3


@case
def feron2023_simplex_test_1():
    """Simplex of Feron et al. (2023) with its published data (exact nodes, CV at 72 190 MPa): self-stress
    mode, linear sensitivity of the level to a strut lengthening, and test 1 by dynamic relaxation
    (level 1.5 kN under self-weight, then 1.3 and 2.1 kN per top node)."""
    end_nodes = np.array([[0, 5], [1, 3], [2, 4], [0, 1], [1, 2], [2, 0],
                          [3, 4], [4, 5], [5, 3], [0, 3], [1, 4], [2, 5]])
    dof = np.array([[False, True, False], [False, False, False], [True, True, False]] + [[True] * 3] * 3)
    youngs = np.zeros((12, 2))
    youngs[0:3, 0] = 70390e6
    youngs[3:9, 1] = 71750e6
    youngs[9:12, 1] = 72190e6
    area = np.r_[np.full(3, 364.4e-6), np.full(9, 50.3e-6)]
    nodes = PyNodes(initial_coordinates=_feron2023_exact_nodes(), dof=dof)
    structure = PyTruss(nodes, PyElements(nodes=nodes, type=np.r_[-np.ones(3), np.ones(9)].astype(int),
                                          end_nodes=end_nodes, area=area, youngs=youngs))
    vs = mp.main_singular_value_decomposition(structure).Vs[:, 0]
    mode = vs / -vs[0]

    prestressed = mp.structure_with_tension(structure, 1500.0 * mode)
    dl = np.zeros(12)
    dl[0] = 1e-3
    lengthened = mp.main_linear_displacement_method(prestressed, np.zeros(18), dl)
    results = {"self_stress_mode": mode,
               "strut_lengthening_tension_change": lengthened.elements.tension - prestressed.elements.tension}

    info = {}
    for label, load in (("self_weight", 0.0), ("p1300", 1300.0), ("p2100", 2100.0)):
        loads = np.zeros((6, 3))
        loads[:3, 2] = -45.7
        loads[3:, 2] = -41.6 - load
        config = PyConfigDR(zero_residual_rtol=1e-6, zero_residual_atol=1e-4, max_time_step=20000, max_ke_reset=2000)
        state = mp.main_dynamic_relaxation(mp.structure_with_tension(structure, 1355.76 * mode), loads,
                                           np.zeros(12), config)
        results[f"{label}_tension"] = state.elements.tension
        results[f"{label}_coordinates"] = state.nodes.coordinates
        results[f"{label}_reactions"] = state.nodes.reactions
        info[label] = _dr_info(config)
    return {"results": results, "info": info}


# ----------------------------------------------------------------------------------------
# Small cable structures (dynamic relaxation)
# ----------------------------------------------------------------------------------------

_CABLE_AREA = np.pi * (8e-3 / 2) ** 2  # m², 8 mm cable


def _dr_cable_config(**kwargs):
    params = dict(zero_residual_rtol=1e-6, zero_residual_atol=1e-6, max_time_step=100, max_ke_reset=20)
    params.update(kwargs)
    return PyConfigDR(**params)


@case
def two_cables_dr_loading():
    """Two horizontal cables loaded vertically at mid-span (large-displacement DR)."""
    nodes = PyNodes(initial_coordinates=np.array([[-2.0, 0, 0], [0, 0, 0], [2.0, 0, 0]]),
                    dof=np.array([[False] * 3, [True, False, True], [False] * 3]))
    elements = PyElements(nodes=nodes, type=np.array([1, 1]), end_nodes=np.array([[0, 1], [1, 2]]),
                          area=np.full(2, _CABLE_AREA), youngs=np.full((2, 2), 70e9))
    loads = np.zeros((3, 3))
    loads[1, 2] = 888.808
    config = _dr_cable_config()
    result = mp.main_dynamic_relaxation(structure=PyTruss(nodes, elements), loads_increment=loads,
                                        free_length_variation=np.zeros(2), config=config)
    return {"results": _truss_results(result), "info": _dr_info(config)}


def _three_cables(top_node, youngs):
    """youngs in MPa."""
    nodes = PyNodes(initial_coordinates=np.array([top_node, [0.0, 0, 0], [4.0, 0, 0], [2.0, 0, 0]]),
                    dof=np.array([[False] * 3] * 3 + [[True] * 3]))
    elements = PyElements(nodes=nodes, type=np.array([1, 1, 1]), end_nodes=np.array([[0, 3], [1, 3], [2, 3]]),
                          area=np.full(3, _CABLE_AREA), youngs=np.array(youngs, dtype=float) * 1e6)  # MPa -> Pa
    return PyTruss(nodes, elements)


@case
def three_cables_dr_prestress():
    """Three cables meeting at one node, prestressed by shortening the vertical cable."""
    structure = _three_cables([2.0, 0, 1.0], [[70000.0] * 3, [70000.0] * 3])
    config = _dr_cable_config()
    result = mp.main_dynamic_relaxation(structure=structure, loads_increment=np.zeros((4, 3)),
                                        free_length_variation=np.array([-126.775e-3, 0, 0]), config=config)
    return {"results": _truss_results(result), "info": _dr_info(config)}


@case
def three_cables_dr_slackening():
    """Three cables, vertical one tension-only: it goes slack under an upward load."""
    structure = _three_cables([2.0, 0, 1.0], [[0.0, 70000.0, 70000.0], [70000.0] * 3])
    loads = np.zeros((4, 3))
    loads[3, 2] = 888.808
    config = _dr_cable_config()
    result = mp.main_dynamic_relaxation(structure=structure, loads_increment=loads,
                                        free_length_variation=np.zeros(3), config=config)
    return {"results": _truss_results(result), "info": _dr_info(config)}


@case
def three_cables_dr_axis_reorientation():
    """Three cables, inclined prestressed cable whose axis reorients (geometric nonlinearity)."""
    structure = _three_cables([0.0, 0, 1.0], [[0.0, 70000.0, 70000.0], [70000.0] * 3])
    config = PyConfigDR(zero_residual_atol=1e-6, max_time_step=150, max_ke_reset=30)
    result = mp.main_dynamic_relaxation(structure=structure, loads_increment=np.zeros((4, 3)),
                                        free_length_variation=np.array([-52.110e-3, 0, 0]), config=config)
    return {"results": _truss_results(result), "info": _dr_info(config)}


# ----------------------------------------------------------------------------------------
# Displacement methods
# ----------------------------------------------------------------------------------------

def _two_bars():
    nodes = PyNodes(initial_coordinates=np.array([[0.0, 0, 0], [1.0, 0, 1.0], [2.0, 0, 0]]),
                    dof=np.array([[False] * 3, [True, False, True], [False] * 3]))
    elements = PyElements(nodes=nodes, type=np.array([-1, -1]), end_nodes=np.array([[0, 1], [1, 2]]),
                          area=np.full(2, 2500e-6), youngs=np.full((2, 2), 10e9), tension=np.zeros(2))
    return PyTruss(nodes, elements)


@case
def two_bars_linear_dm():
    """Two-bar truss, linear displacement method (Feron master thesis, annex A1.1.1)."""
    loads = np.zeros(9)
    loads[5] = -100000.0
    result = mp.main_linear_displacement_method(_two_bars(), loads, np.zeros(2))
    return {"results": _truss_results(result), "info": {}}


@case
def two_bars_nonlinear_dm():
    """Two-bar truss, nonlinear displacement method, same load in 100 steps."""
    loads = np.zeros(9)
    loads[5] = -100000.0
    result = mp.main_nonlinear_displacement_method(_two_bars(), loads, n_steps=100)
    return {"results": _truss_results(result), "info": {}}


@case
def tight_rope_nonlinear_dm_3_stages():
    """Prestressed tight rope: prestress (linear DM) then 2 load stages (nonlinear DM)."""
    nodes = PyNodes(initial_coordinates=np.array([[0.0, 0, 0], [1.0, 0, 0], [2.0, 0, 0]]),
                    dof=np.array([[False] * 3, [True, False, True], [False] * 3]))
    elements = PyElements(nodes=nodes, type=np.array([1, 1]), end_nodes=np.array([[0, 1], [1, 2]]),
                          area=np.full(2, 50e-6), youngs=np.full((2, 2), 100e9))
    prestressed = mp.main_linear_displacement_method(PyTruss(nodes, elements), np.zeros(9),
                                                     np.array([-3.984e-3, -3.984e-3]))
    loads1 = np.zeros(9)
    loads1[5] = -5109.0
    stage1 = mp.main_nonlinear_displacement_method(prestressed, loads1, n_steps=100)
    loads2 = np.zeros(9)
    loads2[5] = -9988.0
    stage2 = mp.main_nonlinear_displacement_method(stage1, loads2 - loads1, n_steps=100)
    results = {f"prestress_{k}": v for k, v in _truss_results(prestressed).items()}
    results.update({f"stage1_{k}": v for k, v in _truss_results(stage1).items()})
    results.update({f"stage2_{k}": v for k, v in _truss_results(stage2).items()})
    return {"results": results, "info": {}}


# ----------------------------------------------------------------------------------------
# Self-stress modes
# ----------------------------------------------------------------------------------------

@case
def snelson_2x_localized_self_stress():
    """2X Snelson structure: SVD then localisation of the self-stress modes (Aloui et al. 2019)."""
    dof = np.array([False, False, False, True, False, True, True, False, True,
                    True, False, True, True, False, True, True, False, False])
    nodes = PyNodes(np.array([[0, 0, 0], [1, 0, -0.2], [0.8, 0, 0.8], [-0.2, 0, 1], [2.2, 0, 1], [2, 0, 0]],
                             dtype=float), dof)
    end_nodes = np.array([[0, 1], [1, 2], [2, 3], [0, 3], [0, 2], [1, 3], [2, 4], [4, 5], [1, 5], [1, 4], [2, 5]])
    elements_type = np.ones(11)
    elements_type[[4, 5, 9, 10]] = -1
    area = np.ones(11)
    area[0:3] = 364.4e-6
    area[3:11] = 50.3e-6
    elements = PyElements(nodes=nodes, end_nodes=end_nodes, type=elements_type,
                          youngs=np.ones((11, 2)) * 70e9, area=area)
    structure = PyTruss(nodes, elements)
    svd = mp.main_singular_value_decomposition(structure)
    localized = mp.localize_self_stress_modes(structure, svd.Vs_T)
    return {"results": {"rank_s_m": np.array([svd.r, svd.s, svd.m], dtype=float),
                        "singular_values": svd.Sr,
                        "localized_self_stress_modes": localized},
            "info": {}}


# ----------------------------------------------------------------------------------------
# Beam FE model (6 DOF/node) - Newton-Raphson and dynamic relaxation
# Same closed-form cases as tests/MuscleCoreTests/Solvers/BeamFEM and BeamDR
# ----------------------------------------------------------------------------------------

_E, _I, _A = 200e9, 8.36e-6, 5.0e-3   # steel, Pa / m4 / m2
_G, _J = _E / (2 * (1 + 0.3)), 2 * 8.36e-6


def _beam_elements(nodes, end_nodes, **kwargs):
    n = len(end_nodes)
    return PyBeamElements(nodes=nodes, end_nodes=np.array(end_nodes), area=np.full(n, _A),
                          youngs=np.full(n, _E), inertia=np.full(n, _I),
                          torsion_constant=np.full(n, _J), shear_modulus=np.full(n, _G), **kwargs)


def _cantilever():
    loads = np.zeros((2, 6))
    loads[1, 1] = 1000.0  # N, transverse tip load
    nodes = PyBeamNodes(initial_coordinates=np.array([[0.0, 0, 0], [3.0, 0, 0]]),
                        dof=np.array([[False] * 6, [True] * 6]), loads=loads)
    return nodes, _beam_elements(nodes, [[0, 1]])


def _beam_results(nodes, elements):
    return {
        "displacements": nodes.displacements,      # [m, rad]
        "reactions": nodes.reactions,              # [N, N.m]
        "tension": elements.tension,               # [N]
        "moment_start": elements.moment_start,     # [N.m]
        "moment_end": elements.moment_end,         # [N.m]
        "torsion": elements.torsion,               # [N.m]
    }


def _beam_info(config):
    return {k: (v.item() if hasattr(v, "item") else v) for k, v in vars(config).items()
            if k in ("converged", "n_iteration", "n_time_step", "n_ke_reset")}


@case
def beam_fem_cantilever_tip_load():
    """Steel cantilever, transverse tip load, corotational Newton-Raphson."""
    nodes, elements = _cantilever()
    nodes, elements, config = mp.main_beam_fem(nodes, elements, PyConfigBeam())
    return {"results": _beam_results(nodes, elements), "info": _beam_info(config)}


@case
def beam_dr_cantilever_tip_load():
    """Steel cantilever, transverse tip load, beam dynamic relaxation."""
    nodes, elements = _cantilever()
    nodes, elements, config = mp.main_beam_dr(nodes, elements, PyConfigBeamDR())
    return {"results": _beam_results(nodes, elements), "info": _beam_info(config)}


@case
def beam_fem_axial_compression():
    """Steel bar in pure axial compression (no bending expected)."""
    loads = np.zeros((2, 6))
    loads[1, 0] = -8200.0
    nodes = PyBeamNodes(initial_coordinates=np.array([[0.0, 0, 0], [3.0, 0, 0]]),
                        dof=np.array([[False] * 6, [True, False, False, True, True, True]]), loads=loads)
    nodes, elements, config = mp.main_beam_fem(nodes, _beam_elements(nodes, [[0, 1]]), PyConfigBeam())
    return {"results": _beam_results(nodes, elements), "info": _beam_info(config)}


@case
def beam_fem_hinge_release():
    """Fixed-fixed two-element beam with a hinge at mid-span, mid-span load."""
    loads = np.zeros((3, 6))
    loads[1, 1] = -500.0
    nodes = PyBeamNodes(initial_coordinates=np.array([[0.0, 0, 0], [2.0, 0, 0], [4.0, 0, 0]]),
                        dof=np.array([[False] * 6, [True] * 6, [False] * 6]), loads=loads)
    elements = _beam_elements(nodes, [[0, 1], [1, 2]], hinge_start=np.array([False, False]),
                              hinge_end=np.array([True, False]))
    nodes, elements, config = mp.main_beam_fem(nodes, elements, PyConfigBeam())
    return {"results": _beam_results(nodes, elements), "info": _beam_info(config)}
