{
    var n = 6;
    var damping = 0.85;
    var iterations = 40;

    var link = [
        0,1,1,0,0,0,
        0,0,1,1,0,0,
        1,0,0,0,0,0,
        0,0,1,0,1,1,
        0,0,1,0,0,1,
        0,0,1,0,0,0
    ];

    var outDeg = new int[n];
    foreach (var i in 0..<n)
    {
        var count = 0;
        foreach (var j in 0..<n) count += link[i * n + j];
        outDeg[i] = count;
    }

    var rank = new double[n];
    var next = new double[n];
    foreach (var i in 0..<n) rank[i] = 1.0 / n;

    foreach (var it in 0..<iterations)
    {
        foreach (var j in 0..<n) next[j] = (1.0 - damping) / n;

        var danglingMass = 0.0;
        foreach (var i in 0..<n)
        {
            if (outDeg[i] == 0) danglingMass += rank[i];
        }

        if (danglingMass > 0.0)
        {
            var spread = damping * danglingMass / n;
            foreach (var j in 0..<n) next[j] += spread;
        }

        foreach (var i in 0..<n)
        {
            if (outDeg[i] == 0) continue;
            var share = damping * rank[i] / outDeg[i];
            foreach (var j in 0..<n)
            {
                if (link[i * n + j] != 0) next[j] += share;
            }
        }

        foreach (var j in 0..<n) rank[j] = next[j];
    }

    var top = 0;
    foreach (var i in 1..<n)
    {
        if (rank[i] > rank[top]) top = i;
    }

    var sum = 0.0;
    foreach (var i in 0..<n) sum += rank[i];

    var rounded = "";
    foreach (var i in 0..<n)
    {
        var r = round(rank[i], 6);
        rounded += r.ToString("0.000000");
        if (i < n - 1) rounded += ",";
    }

    return $"top={top}|sum={round(sum, 6):0.000000}|ranks={rounded}";
}
