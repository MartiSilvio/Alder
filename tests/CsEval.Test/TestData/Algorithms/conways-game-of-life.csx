{
    // Conway's Game of Life: simulate 5 generations on an 8x8 grid
    // Exercises: 2D grid simulation, neighbor counting, double-buffering, nested loops
    var size = 8;
    var cells = size * size;

    // Two buffers for double-buffering
    var grid = new int[64];
    // Glider pattern
    grid[1 * 8 + 2] = 1;
    grid[2 * 8 + 3] = 1;
    grid[3 * 8 + 1] = 1;
    grid[3 * 8 + 2] = 1;
    grid[3 * 8 + 3] = 1;

    var next = new int[64];

    var result = "";

    // Count living cells
    var initialAlive = 0;
    for (var i = 0; i < cells; i++)
    {
        if (grid[i] == 1) initialAlive++;
    }
    result += $"initial={initialAlive}|";

    // Simulate 5 generations
    for (var gen = 0; gen < 5; gen++)
    {
        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                // Count neighbors (wrapping at edges)
                var neighbors = 0;
                for (var dr = -1; dr <= 1; dr++)
                {
                    for (var dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        var nr = (row + dr + size) % size;
                        var nc = (col + dc + size) % size;
                        neighbors += grid[nr * size + nc];
                    }
                }

                var alive = grid[row * size + col];
                // Rules: live cell with 2-3 neighbors survives, dead cell with 3 neighbors is born
                if (alive == 1)
                    next[row * size + col] = (neighbors == 2 || neighbors == 3) ? 1 : 0;
                else
                    next[row * size + col] = (neighbors == 3) ? 1 : 0;
            }
        }

        // Copy next -> grid
        for (var i = 0; i < cells; i++)
        {
            grid[i] = next[i];
        }

        // Count alive cells this generation
        var alive_count = 0;
        for (var i = 0; i < cells; i++)
        {
            if (grid[i] == 1) alive_count++;
        }
        result += $"gen{gen + 1}={alive_count}|";
    }

    // Build final grid visualization (top-left 5x5 corner)
    var viz = "";
    for (var row = 0; row < 5; row++)
    {
        for (var col = 0; col < 5; col++)
        {
            viz += grid[row * size + col] == 1 ? "#" : ".";
        }
        if (row < 4) viz += "/";
    }
    result += $"grid={viz}";

    return result;
}
