{
    // Sudoku solver using backtracking with constraint propagation
    // Exercises: 2D grid, recursion simulation via stack, complex conditionals, modular arithmetic
    // 0 = empty cell

    // The puzzle (medium difficulty)
    var grid = new[] {
        5,3,0, 0,7,0, 0,0,0,
        6,0,0, 1,9,5, 0,0,0,
        0,9,8, 0,0,0, 0,6,0,

        8,0,0, 0,6,0, 0,0,3,
        4,0,0, 8,0,3, 0,0,1,
        7,0,0, 0,2,0, 0,0,6,

        0,6,0, 0,0,0, 2,8,0,
        0,0,0, 4,1,9, 0,0,5,
        0,0,0, 0,8,0, 0,7,9
    };

    // Make a working copy
    var board = new[] {
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0
    };
    for (var i = 0; i < 81; i++) board[i] = grid[i];

    // Count initial empty cells
    var emptyCells = 0;
    for (var i = 0; i < 81; i++)
    {
        if (board[i] == 0) emptyCells++;
    }

    // Iterative backtracking using a stack of (cellIndex, triedValue)
    // Find all empty cell positions
    var emptyPositions = new[] {
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
        -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1
    };
    var numEmpty = 0;
    for (var i = 0; i < 81; i++)
    {
        if (grid[i] == 0)
        {
            emptyPositions[numEmpty] = i;
            numEmpty++;
        }
    }

    // Backtracking: try values 1-9 for each empty cell
    var currentEmpty = 0;
    var solved = false;
    var iterations = 0;

    while (currentEmpty >= 0 && currentEmpty < numEmpty)
    {
        iterations++;
        var pos = emptyPositions[currentEmpty];
        var row = pos / 9;
        var col = pos % 9;
        var currentVal = board[pos];
        var foundValid = false;

        // Try values from currentVal+1 to 9
        for (var tryVal = currentVal + 1; tryVal <= 9; tryVal++)
        {
            // Check if tryVal is valid in this position
            var valid = true;

            // Check row
            for (var c = 0; c < 9 && valid; c++)
            {
                if (c != col && board[row * 9 + c] == tryVal) valid = false;
            }

            // Check column
            for (var r = 0; r < 9 && valid; r++)
            {
                if (r != row && board[r * 9 + col] == tryVal) valid = false;
            }

            // Check 3x3 box
            var boxRow = (row / 3) * 3;
            var boxCol = (col / 3) * 3;
            for (var r = boxRow; r < boxRow + 3 && valid; r++)
            {
                for (var c = boxCol; c < boxCol + 3 && valid; c++)
                {
                    if ((r != row || c != col) && board[r * 9 + c] == tryVal) valid = false;
                }
            }

            if (valid)
            {
                board[pos] = tryVal;
                foundValid = true;
                break;
            }
        }

        if (foundValid)
        {
            currentEmpty++;
            if (currentEmpty == numEmpty) solved = true;
        }
        else
        {
            // Backtrack
            board[pos] = 0;
            currentEmpty--;
        }
    }

    // Verify the solution
    var isValid = solved;
    if (solved)
    {
        // Check all rows
        for (var r = 0; r < 9 && isValid; r++)
        {
            var seen = new[] { false, false, false, false, false, false, false, false, false, false };
            for (var c = 0; c < 9; c++)
            {
                var v = board[r * 9 + c];
                if (v < 1 || v > 9 || seen[v]) { isValid = false; break; }
                seen[v] = true;
            }
        }

        // Check all columns
        for (var c = 0; c < 9 && isValid; c++)
        {
            var seen = new[] { false, false, false, false, false, false, false, false, false, false };
            for (var r = 0; r < 9; r++)
            {
                var v = board[r * 9 + c];
                if (v < 1 || v > 9 || seen[v]) { isValid = false; break; }
                seen[v] = true;
            }
        }

        // Check all 3x3 boxes
        for (var box = 0; box < 9 && isValid; box++)
        {
            var seen = new[] { false, false, false, false, false, false, false, false, false, false };
            var br = (box / 3) * 3;
            var bc = (box % 3) * 3;
            for (var r = br; r < br + 3; r++)
            {
                for (var c = bc; c < bc + 3; c++)
                {
                    var v = board[r * 9 + c];
                    if (v < 1 || v > 9 || seen[v]) { isValid = false; break; }
                    seen[v] = true;
                }
            }
        }
    }

    // Build result
    var result = $"solved={solved}|valid={isValid}|emptyCells={emptyCells}|iterations={iterations}|";

    // First row of solution
    var row0 = "";
    for (var c = 0; c < 9; c++)
    {
        row0 += board[c].ToString();
    }
    result += $"row0={row0}|";

    // Last row of solution
    var row8 = "";
    for (var c = 0; c < 9; c++)
    {
        row8 += board[72 + c].ToString();
    }
    result += $"row8={row8}|";

    // Sum of all cells (should be 405 = 45*9)
    var totalSum = 0;
    for (var i = 0; i < 81; i++) totalSum += board[i];
    result += $"sum={totalSum}";

    return result;
}
