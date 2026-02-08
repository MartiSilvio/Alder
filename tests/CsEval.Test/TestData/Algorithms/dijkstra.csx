{
    // Dijkstra's shortest path algorithm on a weighted graph
    // Exercises: 2D array simulation, nested loops, infinity comparisons, complex state tracking
    // Graph: 5 nodes with weighted edges
    var n = 5;
    var INF = 999999;

    // Adjacency matrix (weights), INF = no edge
    // 0 --(4)--> 1, 0 --(1)--> 2
    // 1 --(1)--> 3
    // 2 --(2)--> 1, 2 --(5)--> 3, 2 --(1)--> 4
    // 3 --(3)--> 4
    // 4 -- none
    var weights = new[] {
        0,   4,   1,   INF, INF,
        INF, 0,   INF, 1,   INF,
        INF, 2,   0,   5,   1,
        INF, INF, INF, 0,   3,
        INF, INF, INF, INF, 0
    };

    // Distance array and visited tracking
    var dist = new int[5];
    for (var i = 0; i < 5; i++) dist[i] = INF;
    var visited = new bool[5];
    var prev = new int[5];
    for (var i = 0; i < 5; i++) prev[i] = -1;

    // Start from node 0
    dist[0] = 0;

    for (var iteration = 0; iteration < n; iteration++)
    {
        // Find unvisited node with minimum distance
        var u = -1;
        var minDist = INF;
        for (var v = 0; v < n; v++)
        {
            if (!visited[v] && dist[v] < minDist)
            {
                minDist = dist[v];
                u = v;
            }
        }

        if (u == -1) break; // All remaining nodes are unreachable

        visited[u] = true;

        // Relax edges from u
        for (var v = 0; v < n; v++)
        {
            var weight = weights[u * n + v];
            if (!visited[v] && weight != INF && dist[u] + weight < dist[v])
            {
                dist[v] = dist[u] + weight;
                prev[v] = u;
            }
        }
    }

    // Build result with distances
    var result = "";
    for (var i = 0; i < n; i++)
    {
        result += $"d[{i}]={dist[i]}";
        if (i < n - 1) result += "|";
    }

    // Reconstruct path from 0 to 4
    var path = "";
    var current = 4;
    while (current != -1)
    {
        if (path.Length > 0) path = "->" + path;
        path = current.ToString() + path;
        current = prev[current];
    }
    result += $"|path04={path}";

    // Reconstruct path from 0 to 3
    var path03 = "";
    current = 3;
    while (current != -1)
    {
        if (path03.Length > 0) path03 = "->" + path03;
        path03 = current.ToString() + path03;
        current = prev[current];
    }
    result += $"|path03={path03}";

    return result;
}
