{
    // A* pathfinding on a 10x10 grid with obstacles
    // Exercises: priority queue simulation, heuristic functions, path reconstruction, complex state
    var rows = 10;
    var cols = 10;
    var cells = rows * cols;

    // Grid: 0=open, 1=wall
    var grid = new[] {
        0,0,0,0,0,0,0,0,0,0,
        0,0,0,1,1,1,0,0,0,0,
        0,0,0,0,0,1,0,0,0,0,
        0,0,1,1,0,1,0,1,1,0,
        0,0,1,0,0,0,0,1,0,0,
        0,0,1,0,1,1,0,0,0,0,
        0,0,0,0,1,0,0,1,1,0,
        0,1,1,0,0,0,0,1,0,0,
        0,0,0,0,1,1,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0
    };

    var startR = 0; var startC = 0;
    var endR = 9; var endC = 9;

    // A* state arrays
    var gScore = new[] {
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999
    };

    var fScore = new[] {
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999,
        999,999,999,999,999,999,999,999,999,999
    };

    var parent = new[] {
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1
    };

    var closed = new[] {
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false,
        false,false,false,false,false,false,false,false,false,false
    };

    // Open set as simple array (linear scan for min f)
    var openSet = new[] {
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1
    };
    var openCount = 0;

    // Manhattan distance heuristic (inline since we can't define methods)
    // h(n) = |row - endR| + |col - endC|

    // Initialize start node
    var startPos = startR * cols + startC;
    var endPos = endR * cols + endC;
    gScore[startPos] = 0;
    var hStart = startR > endR ? startR - endR : endR - startR;
    hStart += startC > endC ? startC - endC : endC - startC;
    fScore[startPos] = hStart;
    openSet[0] = startPos;
    openCount = 1;

    var found = false;
    var nodesExplored = 0;

    // Direction vectors: up, right, down, left
    var dr = new[] { -1, 0, 1, 0 };
    var dc = new[] { 0, 1, 0, -1 };

    while (openCount > 0 && !found)
    {
        // Find node with lowest f score in open set
        var bestIdx = 0;
        var bestF = fScore[openSet[0]];
        for (var i = 1; i < openCount; i++)
        {
            if (fScore[openSet[i]] < bestF)
            {
                bestF = fScore[openSet[i]];
                bestIdx = i;
            }
        }

        var current = openSet[bestIdx];
        // Remove from open set (swap with last)
        openSet[bestIdx] = openSet[openCount - 1];
        openSet[openCount - 1] = -1;
        openCount--;

        if (current == endPos)
        {
            found = true;
            break;
        }

        closed[current] = true;
        nodesExplored++;

        var curR = current / cols;
        var curC = current % cols;

        // Explore neighbors
        for (var d = 0; d < 4; d++)
        {
            var nr = curR + dr[d];
            var nc = curC + dc[d];

            if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;

            var nPos = nr * cols + nc;
            if (grid[nPos] == 1 || closed[nPos]) continue;

            var tentG = gScore[current] + 1;
            if (tentG < gScore[nPos])
            {
                parent[nPos] = current;
                gScore[nPos] = tentG;
                // Manhattan distance heuristic
                var hDist = nr > endR ? nr - endR : endR - nr;
                hDist += nc > endC ? nc - endC : endC - nc;
                fScore[nPos] = tentG + hDist;

                // Add to open set if not already there
                var inOpen = false;
                for (var i = 0; i < openCount; i++)
                {
                    if (openSet[i] == nPos) { inOpen = true; break; }
                }
                if (!inOpen)
                {
                    openSet[openCount] = nPos;
                    openCount++;
                }
            }
        }
    }

    // Reconstruct path
    var pathLen = 0;
    var path = "";
    if (found)
    {
        var cur = endPos;
        while (cur != startPos)
        {
            var pr = cur / cols;
            var pc = cur % cols;
            if (path.Length > 0) path = "->" + path;
            path = $"({pr},{pc})" + path;
            pathLen++;
            cur = parent[cur];
        }
        path = $"({startR},{startC})->" + path;
        pathLen++;
    }

    var result = $"found={found}|pathLen={pathLen}|explored={nodesExplored}|";
    result += $"optimalDist={gScore[endPos]}|";
    result += $"path={path}";

    return result;
}
