// Topological sort using Kahn's algorithm with cycle detection
// Exercises: graph traversal, new int[n], queue simulation, in-degree, cycle detection

var numNodes = 10;
var edgeFrom = new[] { 0, 0, 1, 1, 2, 3, 3, 4, 5, 6, 7, 8 };
var edgeTo   = new[] { 1, 2, 3, 4, 5, 5, 6, 7, 7, 8, 9, 9 };
var numEdges = 12;

// Build adjacency list as flat array with offsets
var outCount = new int[numNodes];
for (var e = 0; e < numEdges; e++)
    outCount[edgeFrom[e]]++;

// Prefix sum for offsets
var offsets = new int[numNodes + 1];
for (var i = 1; i <= numNodes; i++)
    offsets[i] = offsets[i - 1] + outCount[i - 1];

// Fill adjacency list
var adj = new int[numEdges];
var fillPos = new int[numNodes];
for (var e = 0; e < numEdges; e++)
{
    var from = edgeFrom[e];
    adj[offsets[from] + fillPos[from]] = edgeTo[e];
    fillPos[from]++;
}

// Compute in-degrees
var inDegree = new int[numNodes];
for (var e = 0; e < numEdges; e++)
    inDegree[edgeTo[e]]++;

// Kahn's algorithm
var queue = new int[numNodes];
var qHead = 0;
var qTail = 0;

for (var i = 0; i < numNodes; i++)
{
    if (inDegree[i] == 0)
    {
        queue[qTail] = i;
        qTail++;
    }
}

var order = new int[numNodes];
var orderCount = 0;

while (qHead < qTail)
{
    var node = queue[qHead];
    qHead++;
    order[orderCount] = node;
    orderCount++;

    for (var e = offsets[node]; e < offsets[node] + outCount[node]; e++)
    {
        var neighbor = adj[e];
        inDegree[neighbor]--;
        if (inDegree[neighbor] == 0)
        {
            queue[qTail] = neighbor;
            qTail++;
        }
    }
}

var hasCycle = orderCount != numNodes;

// Verify topological ordering
var isValid = true;
if (!hasCycle)
{
    var position = new int[numNodes];
    for (var i = 0; i < orderCount; i++)
        position[order[i]] = i;

    for (var e = 0; e < numEdges; e++)
    {
        if (position[edgeFrom[e]] >= position[edgeTo[e]])
        {
            isValid = false;
            break;
        }
    }
}

// Build order string
var orderStr = "";
for (var i = 0; i < orderCount; i++)
{
    if (i > 0) orderStr += ",";
    orderStr += order[i].ToString();
}

// Longest path (critical path)
var dist = new int[numNodes];
for (var i = 0; i < orderCount; i++)
{
    var node = order[i];
    for (var e = offsets[node]; e < offsets[node] + outCount[node]; e++)
    {
        var neighbor = adj[e];
        var newDist = dist[node] + 1;
        if (newDist > dist[neighbor]) dist[neighbor] = newDist;
    }
}

var criticalPath = 0;
var criticalNode = 0;
for (var i = 0; i < numNodes; i++)
{
    if (dist[i] > criticalPath)
    {
        criticalPath = dist[i];
        criticalNode = i;
    }
}

// Test with a cyclic graph: add edge 9 -> 0
var edgeFrom2 = new[] { 0, 0, 1, 1, 2, 3, 3, 4, 5, 6, 7, 8, 9 };
var edgeTo2   = new[] { 1, 2, 3, 4, 5, 5, 6, 7, 7, 8, 9, 9, 0 };
var numEdges2 = 13;

var inDeg2 = new int[numNodes];
for (var e = 0; e < numEdges2; e++) inDeg2[edgeTo2[e]]++;

var q2 = new int[numNodes];
var q2H = 0;
var q2T = 0;
for (var i = 0; i < numNodes; i++)
{
    if (inDeg2[i] == 0) { q2[q2T] = i; q2T++; }
}

var orderCount2 = 0;
while (q2H < q2T)
{
    var node = q2[q2H]; q2H++;
    orderCount2++;
    for (var e = 0; e < numEdges2; e++)
    {
        if (edgeFrom2[e] == node)
        {
            inDeg2[edgeTo2[e]]--;
            if (inDeg2[edgeTo2[e]] == 0) { q2[q2T] = edgeTo2[e]; q2T++; }
        }
    }
}
var hasCycle2 = orderCount2 != numNodes;

var result = $"nodes={numNodes}|edges={numEdges}|hasCycle={hasCycle}|valid={isValid}|";
result += $"order=[{orderStr}]|critPath={criticalPath}|critNode={criticalNode}|";
result += $"cycleTest={hasCycle2}";

return result;
