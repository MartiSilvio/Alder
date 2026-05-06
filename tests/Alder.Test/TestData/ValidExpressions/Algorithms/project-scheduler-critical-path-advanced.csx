// Advanced C# syntax stress:
// - tuple deconstruction
// - switch expression with relational patterns
// - try/catch when
// - using statement
// - lock statement
// - null-coalescing assignment
//
// Algorithmic workload:
// - topological sort (Kahn)
// - longest-path on DAG (critical path style)
// - slack computation and bucket classification

var n = 10;

// Task durations
var duration = new[] { 5, 3, 4, 6, 2, 7, 3, 4, 5, 2 };

// Directed edges u -> v (u must finish before v starts)
var edges = new[] {
    0,2, 0,3,
    1,3, 1,4,
    2,5, 3,5,
    4,6, 5,7,
    6,8, 7,8,
    8,9
};

var m = edges.Length / 2;

// Build adjacency matrix and indegree
var g = new int[n * n];
for (var i = 0; i < g.Length; i++) g[i] = 0;
var indeg = new int[n];
for (var i = 0; i < n; i++) indeg[i] = 0;

try
{
    for (var e = 0; e < m; e++)
    {
        var u = edges[e * 2];
        var v = edges[e * 2 + 1];
        if (u < 0 || u >= n || v < 0 || v >= n) throw new ArgumentOutOfRangeException("edge");
        if (g[u * n + v] == 0)
        {
            g[u * n + v] = 1;
            indeg[v]++;
        }
    }
}
catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "edge")
{
    return "invalid-graph";
}

// Kahn topological order
var q = new int[n];
var head = 0;
var tail = 0;
for (var i = 0; i < n; i++) if (indeg[i] == 0) q[tail++] = i;

var order = new int[n];
var orderCount = 0;

while (head < tail)
{
    var u = q[head++];
    order[orderCount++] = u;
    for (var v = 0; v < n; v++)
    {
        if (g[u * n + v] == 0) continue;
        indeg[v]--;
        if (indeg[v] == 0) q[tail++] = v;
    }
}

if (orderCount != n)
{
    return "cycle-detected";
}

// Earliest start/finish
var es = new int[n];
var ef = new int[n];
for (var i = 0; i < n; i++) { es[i] = 0; ef[i] = 0; }

for (var i = 0; i < n; i++)
{
    var u = order[i];
    ef[u] = es[u] + duration[u];
    for (var v = 0; v < n; v++)
    {
        if (g[u * n + v] == 0) continue;
        if (ef[u] > es[v]) es[v] = ef[u];
    }
}

var projectFinish = 0;
for (var i = 0; i < n; i++) if (ef[i] > projectFinish) projectFinish = ef[i];

// Latest finish/start backward pass
var lf = new int[n];
var ls = new int[n];
for (var i = 0; i < n; i++)
{
    lf[i] = projectFinish;
    ls[i] = projectFinish - duration[i];
}

for (var idx = n - 1; idx >= 0; idx--)
{
    var u = order[idx];
    var hasSucc = false;
    var best = projectFinish;
    for (var v = 0; v < n; v++)
    {
        if (g[u * n + v] == 0) continue;
        hasSucc = true;
        if (ls[v] < best) best = ls[v];
    }

    if (hasSucc) lf[u] = best;
    ls[u] = lf[u] - duration[u];
}

// Slack and bucket stats using switch expression
var slack = new int[n];
for (var i = 0; i < n; i++) slack[i] = ls[i] - es[i];

var bucketCritical = 0;
var bucketNear = 0;
var bucketLoose = 0;
var riskScore = 0;

for (var i = 0; i < n; i++)
{
    var b = slack[i] switch
    {
        <= 0 => 0,  // critical
        <= 2 => 1,  // near-critical
        _ => 2
    };

    if (b == 0) bucketCritical++;
    else if (b == 1) bucketNear++;
    else bucketLoose++;

    riskScore += (slack[i] switch
    {
        <= 0 => 20,
        <= 2 => 8,
        <= 5 => 3,
        _ => 1
    });
}

// Reconstruct one critical chain by following tasks with zero slack and matching precedence.
var chain = "";
var cur = -1;
for (var i = 0; i < n; i++)
{
    var t = order[i];
    if (slack[t] == 0 && es[t] == 0) { cur = t; break; }
}

var safety = 0;
while (cur != -1 && safety < n + 2)
{
    chain += cur.ToString();
    var nextTask = -1;
    for (var v = 0; v < n; v++)
    {
        if (g[cur * n + v] == 0) continue;
        if (slack[v] == 0 && es[v] == ef[cur])
        {
            nextTask = v;
            break;
        }
    }
    if (nextTask != -1) chain += "->";
    cur = nextTask;
    safety++;
}

// Deconstruction + lock + ??=
object lockObj = new object();
string? summary = null;
lock (lockObj)
{
    var (c, ncrit, l) = (bucketCritical, bucketNear, bucketLoose);
    summary ??= $"C={c},N={ncrit},L={l}";
}

var bytes = 3; // serialized-metadata equivalent without using/IO
var hash = 31;
for (var i = 0; i < n; i++)
{
    hash = hash * 37 + (es[i] + 3) * (i + 5);
    hash = hash * 41 + (ls[i] + 7);
}

return $"finish={projectFinish}|risk={riskScore}|{summary}|chain={chain}|bytes={bytes}|hash={hash}";
