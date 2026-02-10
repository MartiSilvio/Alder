{
    // Maze solver using DFS with backtracking
    // Exercises: 2D grid navigation, stack-based DFS, path reconstruction, complex state management
    // 0=open, 1=wall, 2=visited, 3=path
    var rows = 7;
    var cols = 7;

    var maze = new[] {
        0,1,0,0,0,1,0,
        0,1,0,1,0,1,0,
        0,0,0,1,0,0,0,
        1,1,0,0,0,1,1,
        0,0,0,1,0,0,0,
        0,1,1,1,1,1,0,
        0,0,0,0,0,0,0
    };

    // DFS stack (store row*cols+col)
    var stack = new int[49];
    var parent = new int[49];
    for (var i = 0; i < 49; i++) parent[i] = -1;
    var visited = new bool[49];

    var startR = 0; var startC = 0;
    var endR = 6; var endC = 6;
    var start = startR * cols + startC;
    var end = endR * cols + endC;

    // Push start
    var top = 0;
    stack[top] = start;
    parent[start] = start;
    top++;
    visited[start] = true;

    var found = false;
    // Directions: up, right, down, left
    var dr = new[] { -1, 0, 1, 0 };
    var dc = new[] { 0, 1, 0, -1 };

    while (top > 0 && !found)
    {
        top--;
        var pos = stack[top];
        var r = pos / cols;
        var c = pos % cols;

        if (r == endR && c == endC)
        {
            found = true;
            break;
        }

        // Try all 4 directions
        for (var d = 0; d < 4; d++)
        {
            var nr = r + dr[d];
            var nc = c + dc[d];

            if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
            {
                var npos = nr * cols + nc;
                if (!visited[npos] && maze[npos] == 0)
                {
                    visited[npos] = true;
                    parent[npos] = pos;
                    stack[top] = npos;
                    top++;
                }
            }
        }
    }

    // Reconstruct path
    var path = "";
    var pathLen = 0;
    if (found)
    {
        var cur = end;
        while (cur != start)
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

    var result = $"found={found}|pathLen={pathLen}|path={path}";

    return result;
}
