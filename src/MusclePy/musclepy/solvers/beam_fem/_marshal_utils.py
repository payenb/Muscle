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

"""
BEAM FE MODEL ONLY. Shared array-marshaling helper for py_beam_nodes.py/py_beam_elements.py.

A raw C# 2D array (e.g. bool[,], double[,]) marshaled through Python.NET into a numpy dtype
conversion can come in two different shapes depending on the element type:
- Matching item size (e.g. double[,] vs float64): np.asarray() reads it directly via the
  PEP 3118 buffer protocol, correctly 2D.
- Mismatched item size (e.g. bool[,]: C#'s 4-byte buffer item size vs numpy's 1-byte bool):
  np.asarray() raises, so the input must go through list() first - but .NET's own
  IEnumerable semantics for a multi-dimensional array (T[,]) then yield every scalar
  element in row-major order, NOT rows, i.e. it comes out flat.
Both cases are handled by: try a direct conversion, and if the result isn't already the
right (n, ncols) shape, reshape it - since a real shape MISMATCH (not just "arrived flat")
would still fail via the final shape check the caller performs on the return value.

This is the same defensive pattern already used by musclepy.femodel.PyNodes/PyElements'
_check_and_reshape_array, generalized into a standalone helper since PyBeamNodes/
PyBeamElements are plain classes (not subclasses of PyNodes/PyElements).
"""

import numpy as np


def to_2d_array(arr, ncols, dtype):
    """Convert arr (numpy array, C# 2D array, or nested sequence) to shape (n, ncols)."""
    if isinstance(arr, np.ndarray):
        result = arr.astype(dtype, copy=False)
    else:
        try:
            result = np.asarray(arr, dtype=dtype)
        except Exception:
            # numpy's buffer-protocol failure (PEP 3118 item-size mismatch, bool in
            # particular) does not consistently surface as ValueError/TypeError when the
            # source is a raw C# array marshaled through Python.NET - catch broadly and fall
            # back to the safe, non-buffer-protocol iterator path.
            result = np.array(list(arr), dtype=dtype)

    if result.ndim == 2 and result.shape[1] == ncols:
        return result
    if result.size % ncols == 0:
        return result.reshape(-1, ncols)
    raise ValueError(f"cannot reshape array of size {result.size} to (n,{ncols})")


def to_1d_array(arr, dtype):
    """Convert arr (numpy array, C# 1D array, or sequence) to a 1D array."""
    if isinstance(arr, np.ndarray):
        return arr.astype(dtype, copy=False).reshape(-1)
    try:
        return np.asarray(arr, dtype=dtype).reshape(-1)
    except Exception:
        return np.array(list(arr), dtype=dtype).reshape(-1)
