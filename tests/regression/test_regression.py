"""Non-regression tests: compare musclepy results with the references in tests/reference/.

Run:                      python -m pytest tests/regression
Regenerate references:    python -m pytest tests/regression --update-references
(only after checking that a change of results is intended; commit the JSON diff with it)
"""

import json
import platform
from pathlib import Path

import numpy as np
import pytest

from regression_cases import CASES

REFERENCE_DIR = Path(__file__).resolve().parents[1] / "reference"

# Tolerance on each compared array:
#   |actual - reference| <= RTOL * |reference| + ATOL_SCALE * max(|reference|) + ATOL_FLOOR
# The relative part (1e-5) checks each value; the scale part (1e-6 of the largest value of the
# same array) stops near-zero entries (e.g. reactions on free DOF, moments in cables) from failing
# on round-off; the floor (1e-9 in SI units: 1 nm, 1 nN) covers arrays that are zero everywhere.
# All are far below engineering significance (1e-5 of 1 kN = 0.01 N) yet far above floating-point
# noise: results vary between runs by ~1e-13 relative (multithreaded BLAS), so bit-exact
# comparison is not possible, but any change in the physics or the algorithms shows up.
RTOL = 1e-5
ATOL_SCALE = 1e-6
ATOL_FLOOR = 1e-9


def _to_json_array(value):
    arr = np.asarray(value, dtype=float)
    values = [None if np.isnan(x) else float(x) for x in arr.ravel()]
    return {"shape": list(arr.shape), "values": values}


def _from_json_array(entry):
    values = [np.nan if x is None else x for x in entry["values"]]
    return np.array(values, dtype=float).reshape(entry["shape"])


def _write_reference(path, name, output):
    import musclepy
    import scipy
    doc = {
        "case": name,
        "description": (CASES[name].__doc__ or "").strip(),
        "generated_with": {
            "musclepy": musclepy.__version__,
            "numpy": np.__version__,
            "scipy": scipy.__version__,
            "python": platform.python_version(),
        },
        "tolerance": {"rtol": RTOL, "atol_scale": ATOL_SCALE, "atol_floor": ATOL_FLOOR},
        "info": output["info"],
        "results": {key: _to_json_array(val) for key, val in output["results"].items()},
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(doc, indent=1) + "\n", encoding="utf-8")


@pytest.mark.parametrize("name", sorted(CASES))
def test_matches_reference(name, request):
    output = CASES[name]()
    path = REFERENCE_DIR / f"{name}.json"

    if request.config.getoption("--update-references"):
        _write_reference(path, name, output)
        pytest.skip(f"reference written: {path.name}")

    assert path.exists(), f"missing reference {path}; run pytest with --update-references to create it"
    reference = json.loads(path.read_text(encoding="utf-8"))

    expected_keys = set(reference["results"])
    actual_keys = set(output["results"])
    assert actual_keys == expected_keys, (
        f"result names changed: missing {sorted(expected_keys - actual_keys)}, "
        f"new {sorted(actual_keys - expected_keys)}")

    failures = []
    for key in sorted(expected_keys):
        expected = _from_json_array(reference["results"][key])
        actual = np.asarray(output["results"][key], dtype=float)
        if actual.shape != expected.shape:
            failures.append(f"{key}: shape {actual.shape} != reference {expected.shape}")
            continue
        compared = ~np.isnan(expected)  # NaN in the reference = not compared (e.g. repeated eigenvalues)
        if np.any(np.isnan(actual[compared])):
            failures.append(f"{key}: NaN where the reference has a value")
            continue
        scale = np.max(np.abs(expected[compared])) if np.any(compared) else 0.0
        atol = ATOL_SCALE * scale + ATOL_FLOOR
        diff = np.abs(actual[compared] - expected[compared])
        allowed = RTOL * np.abs(expected[compared]) + atol
        if np.any(diff > allowed):
            worst = np.argmax(diff - allowed)
            failures.append(
                f"{key}: {int(np.sum(diff > allowed))}/{diff.size} values out of tolerance, worst "
                f"{actual[compared][worst]!r} vs reference {expected[compared][worst]!r} "
                f"(|diff|={diff[worst]:.3e}, allowed {allowed[worst]:.3e})")

    assert not failures, f"{name} differs from {path.name}:\n  " + "\n  ".join(failures)
