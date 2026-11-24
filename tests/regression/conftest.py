import os
import sys

# Always test the musclepy source tree of this repository, whatever is installed in the
# active environment (e.g. the "muscle" conda env has a pip-installed copy in site-packages).
_SRC = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "src", "MusclePy"))
if _SRC not in sys.path:
    sys.path.insert(0, _SRC)


def pytest_addoption(parser):
    parser.addoption(
        "--update-references",
        action="store_true",
        default=False,
        help="Recompute the regression cases and overwrite tests/reference/*.json instead of comparing.",
    )


def pytest_report_header(config):
    import musclepy
    import numpy
    import scipy
    return (f"musclepy {musclepy.__version__} from {os.path.dirname(musclepy.__file__)} | "
            f"numpy {numpy.__version__} | scipy {scipy.__version__}")
