// Bellman-Ford shortest paths with negative weights (no negative cycles reachable from source)
// Exercises: relaxation passes, early-stop optimization, negative-cycle detection, try/catch/finally flow

var n = 6;
var source = 0;
var INF = 1_000_000;

// Directed edges as flattened triplets [u,v,w].
var edges = new[] {
    0,1,5,   0,2,4,   1,3,3,   2,1,6,   2,3,2,
    3,4,-2,  4,5,3,   1,5,10,  2,5,12,  0,5,20
};
var m = edges.Length / 3;

var dist = new int[n];
var prev = new int[n];
for (var i = 0; i < n; i++)
{
    dist[i] = INF;
    prev[i] = -1;
}
dist[source] = 0;

var relaxations = 0;
var negativeCycle = false;
var finalized = false;

try
{
    for (var pass = 0; pass < n - 1; pass++)
    {
        var changed = false;
        for (var e = 0; e < m; e++)
        {
            var u = edges[e * 3];
            var v = edges[e * 3 + 1];
            var w = edges[e * 3 + 2];
            if (dist[u] == INF) continue;

            var cand = dist[u] + w;
            if (cand < dist[v])
            {
                dist[v] = cand;
                prev[v] = u;
                relaxations++;
                changed = true;
            }
        }
        if (!changed) break;
    }

    // Detect negative cycle reachable from source.
    for (var e = 0; e < m; e++)
    {
        var u = edges[e * 3];
        var v = edges[e * 3 + 1];
        var w = edges[e * 3 + 2];
        if (dist[u] != INF && dist[u] + w < dist[v])
        {
            negativeCycle = true;
            throw new InvalidOperationException("Negative cycle detected");
        }
    }
}
catch (InvalidOperationException)
{
    // Keep deterministic output; cycle flag already set.
}
finally
{
    finalized = true;
}

var pathTo5 = "";
var cur = 5;
var hop = 0;
while (cur != -1 && hop < 16)
{
    if (pathTo5.Length > 0) pathTo5 = "->" + pathTo5;
    pathTo5 = cur.ToString() + pathTo5;
    cur = prev[cur];
    hop++;
}

var d = "";
for (var i = 0; i < n; i++)
{
    d += dist[i].ToString();
    if (i < n - 1) d += ",";
}

return $"negCycle={negativeCycle}|finalized={finalized}|relax={relaxations}|dist={d}|path05={pathTo5}";
