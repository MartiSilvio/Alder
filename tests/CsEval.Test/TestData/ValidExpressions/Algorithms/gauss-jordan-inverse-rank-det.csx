{
    // Gauss-Jordan elimination:
    // - matrix inverse
    // - determinant
    // - rank
    // - residual check A * A^-1 ~= I

    var n = 5;
    var A = new[] {
        4.0,  2.0,  1.0,  3.0,  0.0,
        0.0,  5.0, -1.0,  2.0,  1.0,
        2.0,  0.0,  6.0,  1.0, -2.0,
        1.0,  2.0,  0.0,  7.0,  3.0,
        3.0, -1.0,  2.0,  0.0,  8.0
    };

    // Augmented matrix [A | I]
    var width = n * 2;
    var aug = new double[n * width];
    for (var r = 0; r < n; r++)
    {
        for (var c = 0; c < n; c++) aug[r * width + c] = A[r * n + c];
        for (var c = 0; c < n; c++) aug[r * width + (n + c)] = (r == c) ? 1.0 : 0.0;
    }

    var det = 1.0;
    var sign = 1.0;
    var rank = 0;
    var eps = 1e-10;

    for (var col = 0; col < n; col++)
    {
        // Pivot search
        var pivotRow = -1;
        var bestAbs = 0.0;
        for (var r = col; r < n; r++)
        {
            var v = Math.Abs(aug[r * width + col]);
            if (v > bestAbs)
            {
                bestAbs = v;
                pivotRow = r;
            }
        }

        if (pivotRow == -1 || bestAbs < eps)
        {
            det = 0.0;
            continue;
        }

        rank++;

        if (pivotRow != col)
        {
            for (var c = 0; c < width; c++)
            {
                var t = aug[col * width + c];
                aug[col * width + c] = aug[pivotRow * width + c];
                aug[pivotRow * width + c] = t;
            }
            sign = -sign;
        }

        var pivot = aug[col * width + col];
        det *= pivot;

        // Normalize pivot row
        for (var c = 0; c < width; c++) aug[col * width + c] /= pivot;

        // Eliminate all other rows
        for (var r = 0; r < n; r++)
        {
            if (r == col) continue;
            var factor = aug[r * width + col];
            if (Math.Abs(factor) < eps) continue;
            for (var c = 0; c < width; c++)
            {
                aug[r * width + c] -= factor * aug[col * width + c];
            }
        }
    }

    det *= sign;

    // Extract inverse from right side.
    var inv = new double[n * n];
    for (var r = 0; r < n; r++)
    {
        for (var c = 0; c < n; c++)
        {
            inv[r * n + c] = aug[r * width + (n + c)];
        }
    }

    // Residual error of A*inv vs I
    var residual = 0.0;
    for (var r = 0; r < n; r++)
    {
        for (var c = 0; c < n; c++)
        {
            var sum = 0.0;
            for (var k = 0; k < n; k++) sum += A[r * n + k] * inv[k * n + c];
            var expected = (r == c) ? 1.0 : 0.0;
            residual += Math.Abs(sum - expected);
        }
    }

    var traceInv = 0.0;
    for (var i = 0; i < n; i++) traceInv += inv[i * n + i];

    return $"rank={rank}|det={Math.Round(det, 6):0.000000}|traceInv={Math.Round(traceInv, 6):0.000000}|res={Math.Round(residual, 8):0.00000000}";
}
