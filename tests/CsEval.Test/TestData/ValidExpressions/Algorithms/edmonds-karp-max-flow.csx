{
    // Edmonds-Karp maximum flow (BFS augmenting paths)
    // Exercises: residual graph updates, BFS parent reconstruction, nested loops, queue simulation

    var n = 6; // 0=source, 5=sink
    var cap = new[] {
        // 0  1  2  3  4  5
           0,16,13, 0, 0, 0, // 0
           0, 0,10,12, 0, 0, // 1
           0, 4, 0, 0,14, 0, // 2
           0, 0, 9, 0, 0,20, // 3
           0, 0, 0, 7, 0, 4, // 4
           0, 0, 0, 0, 0, 0  // 5
    };

    var residual = new int[n * n];
    for (var i = 0; i < n * n; i++) residual[i] = cap[i];

    var parent = new int[n];
    var maxFlow = 0;
    var augmentCount = 0;

    while (true)
    {
        for (var i = 0; i < n; i++) parent[i] = -1;
        parent[0] = -2;

        var q = new int[n];
        var head = 0;
        var tail = 0;
        q[tail++] = 0;

        var pathFlow = new int[n];
        for (var i = 0; i < n; i++) pathFlow[i] = 0;
        pathFlow[0] = int.MaxValue;

        var foundSink = false;
        while (head < tail && !foundSink)
        {
            var u = q[head++];
            for (var v = 0; v < n; v++)
            {
                var c = residual[u * n + v];
                if (parent[v] == -1 && c > 0)
                {
                    parent[v] = u;
                    pathFlow[v] = Math.Min(pathFlow[u], c);
                    if (v == 5)
                    {
                        foundSink = true;
                        break;
                    }
                    q[tail++] = v;
                }
            }
        }

        if (!foundSink) break;

        var flow = pathFlow[5];
        maxFlow += flow;
        augmentCount++;

        var vtx = 5;
        while (vtx != 0)
        {
            var u = parent[vtx];
            residual[u * n + vtx] -= flow;
            residual[vtx * n + u] += flow;
            vtx = u;
        }
    }

    // Outgoing flow from source in final residual can be reconstructed as original-capacity minus residual.
    var sourceOut = 0;
    for (var v = 0; v < n; v++)
    {
        var used = cap[0 * n + v] - residual[0 * n + v];
        if (used > 0) sourceOut += used;
    }

    // Incoming flow into sink.
    var sinkIn = 0;
    for (var u = 0; u < n; u++)
    {
        var used = cap[u * n + 5] - residual[u * n + 5];
        if (used > 0) sinkIn += used;
    }

    return $"maxFlow={maxFlow}|augments={augmentCount}|sourceOut={sourceOut}|sinkIn={sinkIn}";
}
