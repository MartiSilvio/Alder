{
    // Kosaraju SCC with iterative DFS and condensation edge counting.

    var n = 12;
    var edges = new[] {
        0,1, 1,2, 2,0, 2,3, 3,4, 4,5, 5,3, 5,6, 6,7, 7,8, 8,6,
        8,9, 9,10, 10,11, 11,9, 1,7, 4,10, 0,6, 2,8
    };

    var g = new int[n * n];
    var gr = new int[n * n];
    for (var i = 0; i < g.Length; i++) { g[i] = 0; gr[i] = 0; }

    for (var e = 0; e < edges.Length / 2; e++)
    {
        var u = edges[e * 2];
        var v = edges[e * 2 + 1];
        g[u * n + v] = 1;
        gr[v * n + u] = 1;
    }

    // First pass: finish order on original graph.
    var visited = new bool[n];
    for (var i = 0; i < n; i++) visited[i] = false;

    var order = new int[n];
    var orderCount = 0;

    var stackNode = new int[n * 2];
    var stackNext = new int[n * 2];

    for (var start = 0; start < n; start++)
    {
        if (visited[start]) continue;

        var sp = 0;
        stackNode[sp] = start;
        stackNext[sp] = 0;
        visited[start] = true;
        sp++;

        while (sp > 0)
        {
            var top = sp - 1;
            var u = stackNode[top];
            var nextNeighbor = stackNext[top];

            var advanced = false;
            for (var v = nextNeighbor; v < n; v++)
            {
                stackNext[top] = v + 1;
                if (g[u * n + v] != 0 && !visited[v])
                {
                    visited[v] = true;
                    stackNode[sp] = v;
                    stackNext[sp] = 0;
                    sp++;
                    advanced = true;
                    break;
                }
            }

            if (!advanced)
            {
                order[orderCount++] = u;
                sp--;
            }
        }
    }

    // Second pass on reversed graph in reverse finish order.
    var comp = new int[n];
    for (var i = 0; i < n; i++) comp[i] = -1;

    var compCount = 0;
    var compSizes = new int[n];
    for (var i = 0; i < n; i++) compSizes[i] = 0;

    for (var idx = orderCount - 1; idx >= 0; idx--)
    {
        var start = order[idx];
        if (comp[start] != -1) continue;

        var q = new int[n];
        var head = 0;
        var tail = 0;
        q[tail++] = start;
        comp[start] = compCount;
        compSizes[compCount]++;

        while (head < tail)
        {
            var u = q[head++];
            for (var v = 0; v < n; v++)
            {
                if (gr[u * n + v] != 0 && comp[v] == -1)
                {
                    comp[v] = compCount;
                    compSizes[compCount]++;
                    q[tail++] = v;
                }
            }
        }

        compCount++;
    }

    // Condensation DAG edge count.
    var dag = new int[compCount * compCount];
    for (var i = 0; i < dag.Length; i++) dag[i] = 0;

    for (var u = 0; u < n; u++)
    {
        for (var v = 0; v < n; v++)
        {
            if (g[u * n + v] == 0) continue;
            var cu = comp[u];
            var cv = comp[v];
            if (cu != cv) dag[cu * compCount + cv] = 1;
        }
    }

    var dagEdges = 0;
    for (var i = 0; i < dag.Length; i++) dagEdges += dag[i];

    // Component signature sorted by component id.
    var sig = "";
    var maxSize = 0;
    for (var c = 0; c < compCount; c++)
    {
        if (compSizes[c] > maxSize) maxSize = compSizes[c];
        sig += compSizes[c];
        if (c < compCount - 1) sig += "-";
    }

    // Node->component mapping hash.
    var mapHash = 23;
    for (var i = 0; i < n; i++) mapHash = mapHash * 37 + comp[i] * (i + 5);

    return $"scc={compCount}|max={maxSize}|dagEdges={dagEdges}|sizes={sig}|hash={mapHash}";
}
