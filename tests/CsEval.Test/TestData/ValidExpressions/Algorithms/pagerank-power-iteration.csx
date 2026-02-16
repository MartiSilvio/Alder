{
    // PageRank (power iteration) on a small directed graph
    // Exercises: iterative numeric updates, damping factor, dangling handling, normalization checks

    var n = 6;
    var damping = 0.85;
    var iterations = 40;

    // Adjacency matrix (from -> to), flattened row-major.
    var link = new[] {
        // 0 -> 1,2
        0,1,1,0,0,0,
        // 1 -> 2,3
        0,0,1,1,0,0,
        // 2 -> 0
        1,0,0,0,0,0,
        // 3 -> 2,4,5
        0,0,1,0,1,1,
        // 4 -> 2,5
        0,0,1,0,0,1,
        // 5 -> 2
        0,0,1,0,0,0
    };

    var outDeg = new int[n];
    for (var i = 0; i < n; i++)
    {
        var count = 0;
        for (var j = 0; j < n; j++) count += link[i * n + j];
        outDeg[i] = count;
    }

    var rank = new double[n];
    var next = new double[n];
    for (var i = 0; i < n; i++) rank[i] = 1.0 / n;

    for (var it = 0; it < iterations; it++)
    {
        for (var j = 0; j < n; j++) next[j] = (1.0 - damping) / n;

        var danglingMass = 0.0;
        for (var i = 0; i < n; i++)
        {
            if (outDeg[i] == 0) danglingMass += rank[i];
        }

        if (danglingMass > 0.0)
        {
            var spread = damping * danglingMass / n;
            for (var j = 0; j < n; j++) next[j] += spread;
        }

        for (var i = 0; i < n; i++)
        {
            if (outDeg[i] == 0) continue;
            var share = damping * rank[i] / outDeg[i];
            for (var j = 0; j < n; j++)
            {
                if (link[i * n + j] != 0) next[j] += share;
            }
        }

        for (var j = 0; j < n; j++) rank[j] = next[j];
    }

    // Find top-ranked page.
    var top = 0;
    for (var i = 1; i < n; i++)
    {
        if (rank[i] > rank[top]) top = i;
    }

    // Normalize check and compact fingerprint.
    var sum = 0.0;
    for (var i = 0; i < n; i++) sum += rank[i];

    var rounded = "";
    for (var i = 0; i < n; i++)
    {
        var r = Math.Round(rank[i], 6);
        rounded += r.ToString("0.000000");
        if (i < n - 1) rounded += ",";
    }

    return $"top={top}|sum={Math.Round(sum, 6):0.000000}|ranks={rounded}";
}
