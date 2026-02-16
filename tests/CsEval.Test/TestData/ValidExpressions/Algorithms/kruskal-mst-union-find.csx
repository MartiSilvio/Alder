{
    // Kruskal minimum spanning tree with union-find (disjoint set union)
    // Exercises: sorting, flattened edge arrays, path compression, union by rank

    // Undirected weighted graph with 8 vertices and 14 edges.
    // Edge format: [u, v, w]
    var edges = new[] {
        0,1,4,  0,2,3,  1,2,1,  1,3,2,  2,3,4,  3,4,2,  4,5,6,
        2,5,7,  1,5,8,  0,6,10, 6,7,3,  5,7,1,  3,7,5,  4,7,4
    };

    var vertexCount = 8;
    var edgeCount = edges.Length / 3;

    // Stable in-place sort by edge weight (selection sort, deterministic).
    for (var i = 0; i < edgeCount - 1; i++)
    {
        var minIdx = i;
        var minWeight = edges[minIdx * 3 + 2];
        for (var j = i + 1; j < edgeCount; j++)
        {
            var w = edges[j * 3 + 2];
            if (w < minWeight)
            {
                minWeight = w;
                minIdx = j;
            }
        }

        if (minIdx != i)
        {
            var a0 = edges[i * 3];
            var a1 = edges[i * 3 + 1];
            var a2 = edges[i * 3 + 2];

            edges[i * 3] = edges[minIdx * 3];
            edges[i * 3 + 1] = edges[minIdx * 3 + 1];
            edges[i * 3 + 2] = edges[minIdx * 3 + 2];

            edges[minIdx * 3] = a0;
            edges[minIdx * 3 + 1] = a1;
            edges[minIdx * 3 + 2] = a2;
        }
    }

    var parent = new int[vertexCount];
    var rank = new int[vertexCount];
    for (var i = 0; i < vertexCount; i++)
    {
        parent[i] = i;
        rank[i] = 0;
    }

    // Find with path compression.
    var selectedU = new int[vertexCount - 1];
    var selectedV = new int[vertexCount - 1];
    var selectedW = new int[vertexCount - 1];
    for (var i = 0; i < vertexCount - 1; i++)
    {
        selectedU[i] = -1;
        selectedV[i] = -1;
        selectedW[i] = -1;
    }

    var selectedCount = 0;
    var totalWeight = 0;

    for (var e = 0; e < edgeCount && selectedCount < vertexCount - 1; e++)
    {
        var u = edges[e * 3];
        var v = edges[e * 3 + 1];
        var w = edges[e * 3 + 2];

        // Find root u
        var ru = u;
        while (parent[ru] != ru) ru = parent[ru];
        var x = u;
        while (parent[x] != x)
        {
            var next = parent[x];
            parent[x] = ru;
            x = next;
        }

        // Find root v
        var rv = v;
        while (parent[rv] != rv) rv = parent[rv];
        x = v;
        while (parent[x] != x)
        {
            var next = parent[x];
            parent[x] = rv;
            x = next;
        }

        if (ru == rv) continue; // would form cycle

        // Union by rank
        if (rank[ru] < rank[rv]) parent[ru] = rv;
        else if (rank[ru] > rank[rv]) parent[rv] = ru;
        else
        {
            parent[rv] = ru;
            rank[ru]++;
        }

        selectedU[selectedCount] = u;
        selectedV[selectedCount] = v;
        selectedW[selectedCount] = w;
        selectedCount++;
        totalWeight += w;
    }

    // Verify connectivity by checking one representative root.
    var rep = 0;
    while (parent[rep] != rep) rep = parent[rep];
    var connected = true;
    for (var i = 1; i < vertexCount; i++)
    {
        var r = i;
        while (parent[r] != r) r = parent[r];
        if (r != rep) { connected = false; break; }
    }

    var result = $"selected={selectedCount}|weight={totalWeight}|connected={connected}|edges=";
    for (var i = 0; i < selectedCount; i++)
    {
        result += $"{selectedU[i]}-{selectedV[i]}:{selectedW[i]}";
        if (i < selectedCount - 1) result += ",";
    }

    return result;
}
