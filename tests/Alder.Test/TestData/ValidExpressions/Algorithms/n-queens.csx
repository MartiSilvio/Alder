// N-Queens solver: finds all solutions for 8-queens
// Exercises: Math.Abs, iterative backtracking, diagonal checking, solution counting/validation

var n = 8;
// queens[row] = column where queen is placed (-1 = not placed yet)
var queens = new int[8];
for (var i = 0; i < 8; i++) queens[i] = -1;
var solutionCount = 0;

// Store first and last solutions for verification
var firstSolution = "";
var lastSolution = "";

// All-solutions stats
var totalQueenPlacements = 0;
var totalBacktracks = 0;

// Iterative backtracking
var row = 0;
while (row >= 0)
{
    var col = queens[row] + 1; // try next column (or 0 if fresh)
    var placed = false;

    while (col < n)
    {
        totalQueenPlacements++;
        // Check if placing queen at (row, col) is safe
        var safe = true;
        for (var prevRow = 0; prevRow < row; prevRow++)
        {
            var prevCol = queens[prevRow];
            if (prevCol == col) { safe = false; break; }
            var rowDiff = row - prevRow;
            var colDiff = col - prevCol;
            if (colDiff < 0) colDiff = -colDiff;
            if (rowDiff == colDiff) { safe = false; break; }
        }

        if (safe)
        {
            queens[row] = col;
            placed = true;
            break;
        }
        col++;
    }

    if (placed)
    {
        if (row == n - 1)
        {
            // Found a complete solution
            solutionCount++;
            var sol = "";
            for (var i = 0; i < n; i++)
            {
                sol += queens[i].ToString();
                if (i < n - 1) sol += ",";
            }
            if (solutionCount == 1) firstSolution = sol;
            lastSolution = sol;

            // Backtrack from last row to find more solutions
            queens[row] = -1;
            row--;
            if (row >= 0)
            {
                // Continue searching from current position
            }
            else break;
        }
        else
        {
            row++;
        }
    }
    else
    {
        totalBacktracks++;
        queens[row] = -1;
        row--;
    }
}

// Known facts about 8-queens
var expectedSolutions = 92;
var countCorrect = solutionCount == expectedSolutions;

// Compute attack-free verification on a board from the first solution
// First solution for 8-queens (lexicographic) is 0,4,7,5,2,6,1,3
var expectedFirst = "0,4,7,5,2,6,1,3";
var firstCorrect = firstSolution == expectedFirst;

var result = $"n={n}|solutions={solutionCount}|countCorrect={countCorrect}|";
result += $"firstCorrect={firstCorrect}|first={firstSolution}|last={lastSolution}|";
result += $"placements={totalQueenPlacements}|backtracks={totalBacktracks}";

return result;
