{
    // Extreme algorithm: Hungarian method (O(n^3)) for minimum-cost assignment.
    // Advanced syntax included: tuple deconstruction, switch expressions, catch when, lock, ??=

    var n = 10;
    var cost = new[] {
        82, 83, 69, 92, 77, 37, 49, 92, 57, 65,
        77, 37, 49, 92, 57, 65, 82, 83, 69, 92,
        11, 69,  5, 86, 35, 61, 14, 17, 40, 45,
         8,  9, 98, 23, 74, 13,  2, 46, 71, 19,
        27, 20, 38, 49, 50, 94, 86, 39, 61, 73,
        55, 44, 13, 24, 95, 61, 44, 69, 38, 54,
        66, 15, 82, 31, 24, 27, 62, 80, 70, 33,
        47, 65, 29, 70, 95, 33, 27, 50, 41, 22,
        73, 51, 64, 46, 38, 59, 79, 21, 32, 18,
        62, 26, 48, 71, 55, 40, 12, 28, 35, 44
    };

    try
    {
        if (cost.Length != n * n) throw new ArgumentException("bad-shape");
    }
    catch (ArgumentException ex) when (ex.Message.Contains("bad"))
    {
        return "invalid-cost-matrix";
    }

    // Hungarian implementation (1-indexed helper arrays).
    var u = new int[n + 1];
    var v = new int[n + 1];
    var p = new int[n + 1];
    var way = new int[n + 1];

    for (var i = 0; i <= n; i++)
    {
        u[i] = 0;
        v[i] = 0;
        p[i] = 0;
        way[i] = 0;
    }

    for (var i = 1; i <= n; i++)
    {
        p[0] = i;
        var minv = new int[n + 1];
        var used = new bool[n + 1];
        for (var j = 0; j <= n; j++)
        {
            minv[j] = 1_000_000_000;
            used[j] = false;
        }

        var j0 = 0;
        do
        {
            used[j0] = true;
            var i0 = p[j0];
            var delta = 1_000_000_000;
            var j1 = 0;

            for (var j = 1; j <= n; j++)
            {
                if (used[j]) continue;
                var cur = cost[(i0 - 1) * n + (j - 1)] - u[i0] - v[j];
                if (cur < minv[j])
                {
                    minv[j] = cur;
                    way[j] = j0;
                }
                if (minv[j] < delta)
                {
                    delta = minv[j];
                    j1 = j;
                }
            }

            for (var j = 0; j <= n; j++)
            {
                if (used[j])
                {
                    u[p[j]] += delta;
                    v[j] -= delta;
                }
                else
                {
                    minv[j] -= delta;
                }
            }

            j0 = j1;
        } while (p[j0] != 0);

        // Augmenting path restore
        do
        {
            var j1 = way[j0];
            p[j0] = p[j1];
            j0 = j1;
        } while (j0 != 0);
    }

    // assignment[row] = col
    var assignment = new int[n];
    for (var i = 0; i < n; i++) assignment[i] = -1;

    for (var j = 1; j <= n; j++)
    {
        var row = p[j] - 1;
        var col = j - 1;
        assignment[row] = col;
    }

    var total = 0;
    for (var i = 0; i < n; i++) total += cost[i * n + assignment[i]];

    // Verify uniqueness of columns
    var colUsed = new bool[n];
    for (var i = 0; i < n; i++) colUsed[i] = false;
    var unique = true;
    for (var i = 0; i < n; i++)
    {
        if (assignment[i] < 0 || assignment[i] >= n || colUsed[assignment[i]])
        {
            unique = false;
            break;
        }
        colUsed[assignment[i]] = true;
    }

    // Severity classification via switch expression
    var quality = (total / n) switch
    {
        <= 20 => "excellent",
        <= 35 => "good",
        <= 50 => "ok",
        _ => "high"
    };

    // Deconstruction + lock + ??=
    object lockObj = new object();
    string? digest = null;
    lock (lockObj)
    {
        var (a, b, c) = (u[0], v[0], p[0]);
        digest ??= $"u0={a},v0={b},p0={c}";
    }

    var pairStr = "";
    var hash = 29;
    for (var i = 0; i < n; i++)
    {
        pairStr += i + "->" + assignment[i];
        if (i < n - 1) pairStr += ",";
        hash = hash * 31 + (i + 3) * (assignment[i] + 5);
    }

    return $"total={total}|unique={unique}|quality={quality}|{digest}|hash={hash}|{pairStr}";
}
