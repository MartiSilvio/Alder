// Floyd-Warshall all-pairs shortest path
// Exercises: triple nested loops, matrix flattening, infinity guarding, transitive closure style updates

var n = 6;
var INF = 1000000;

// Directed graph adjacency matrix.
var dist = new[] {
    0,   7,   9,   INF, INF, 14,
    INF, 0,   10,  15,  INF, INF,
    INF, INF, 0,   11,  INF, 2,
    INF, INF, INF, 0,   6,   INF,
    INF, INF, INF, INF, 0,   9,
    INF, INF, INF, INF, INF, 0
};

// Compute APSP
for (var k = 0; k < n; k++)
{
    for (var i = 0; i < n; i++)
    {
        var ik = dist[i * n + k];
        if (ik == INF) continue;

        for (var j = 0; j < n; j++)
        {
            var kj = dist[k * n + j];
            if (kj == INF) continue;

            var candidate = ik + kj;
            var idx = i * n + j;
            if (candidate < dist[idx]) dist[idx] = candidate;
        }
    }
}

// Collect selected distances and consistency checks.
var d03 = dist[0 * n + 3];
var d04 = dist[0 * n + 4];
var d13 = dist[1 * n + 3];
var d35 = dist[3 * n + 5];

// Triangle-inequality spot checks
var tri1 = dist[0 * n + 3] <= dist[0 * n + 2] + dist[2 * n + 3];
var tri2 = dist[0 * n + 4] <= dist[0 * n + 3] + dist[3 * n + 4];
var tri3 = dist[1 * n + 5] <= dist[1 * n + 2] + dist[2 * n + 5];

// Sum finite distances for a compact fingerprint.
var sumFinite = 0;
var reachablePairs = 0;
for (var i = 0; i < n; i++)
{
    for (var j = 0; j < n; j++)
    {
        var d = dist[i * n + j];
        if (d != INF)
        {
            sumFinite += d;
            reachablePairs++;
        }
    }
}

return $"d03={d03}|d04={d04}|d13={d13}|d35={d35}|tri={tri1},{tri2},{tri3}|reachable={reachablePairs}|sum={sumFinite}";
