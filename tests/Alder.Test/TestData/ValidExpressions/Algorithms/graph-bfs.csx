// BFS on a directed graph using adjacency matrix
// Exercises: 2D array simulation, queue via array, nested loops, string interpolation
// Graph: 6 nodes, edges: 0->1, 0->2, 1->3, 2->3, 2->4, 3->5, 4->5
var nodeCount = 6;

// Adjacency matrix as flat array (row-major)
var adj = new[] {
    0,1,1,0,0,0,
    0,0,0,1,0,0,
    0,0,0,1,1,0,
    0,0,0,0,0,1,
    0,0,0,0,0,1,
    0,0,0,0,0,0
};

// BFS state arrays
var visited = new bool[6];
var queue = new int[6];
var distances = new int[6];
for (var i = 0; i < 6; i++) distances[i] = -1;

var qHead = 0;
var qTail = 0;

// Start BFS from node 0
queue[qTail] = 0;
qTail++;
visited[0] = true;
distances[0] = 0;

var visitOrder = "";
while (qHead < qTail)
{
    var current = queue[qHead];
    qHead++;

    if (visitOrder.Length > 0) visitOrder += ",";
    visitOrder += current.ToString();

    // Visit all unvisited neighbors
    for (var neighbor = 0; neighbor < nodeCount; neighbor++)
    {
        if (adj[current * nodeCount + neighbor] == 1 && !visited[neighbor])
        {
            visited[neighbor] = true;
            distances[neighbor] = distances[current] + 1;
            queue[qTail] = neighbor;
            qTail++;
        }
    }
}

// Build result
var result = $"order={visitOrder}|";

var distStr = "";
for (var i = 0; i < nodeCount; i++)
{
    if (i > 0) distStr += ",";
    distStr += distances[i].ToString();
}
result += $"distances={distStr}|";

// Check all nodes reachable
var allReachable = true;
for (var i = 0; i < nodeCount; i++)
{
    if (!visited[i]) { allReachable = false; break; }
}
result += $"allReachable={allReachable}|";
result += $"shortestTo5={distances[5]}";

return result;
