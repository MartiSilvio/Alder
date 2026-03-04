// Minimum-cost assignment (8x8) using bitmask DP + parent reconstruction.

var n = 8;
var cost = new[] {
    90, 76, 75, 70, 50, 74, 12, 68,
    35, 85, 55, 65, 48, 101, 70, 83,
    125, 95, 90, 105, 59, 120, 36, 73,
    45, 110, 95, 115, 104, 83, 37, 71,
    60, 105, 80, 75, 59, 62, 93, 88,
    45, 65, 110, 95, 47, 31, 81, 34,
    38, 51, 107, 41, 69, 99, 115, 48,
    47, 85, 57, 71, 92, 77, 109, 36
};

var maxMask = 1 << n;
var INF = 1_000_000_000;

var dp = new int[maxMask];
var parentMask = new int[maxMask];
var parentCol = new int[maxMask];
for (var m = 0; m < maxMask; m++)
{
    dp[m] = INF;
    parentMask[m] = -1;
    parentCol[m] = -1;
}
dp[0] = 0;

// Precompute popcount for each mask.
var pop = new int[maxMask];
pop[0] = 0;
for (var m = 1; m < maxMask; m++) pop[m] = pop[m >> 1] + (m & 1);

for (var mask = 0; mask < maxMask; mask++)
{
    var row = pop[mask];
    if (row >= n || dp[mask] == INF) continue;

    for (var col = 0; col < n; col++)
    {
        if ((mask & (1 << col)) != 0) continue;
        var nextMask = mask | (1 << col);
        var candidate = dp[mask] + cost[row * n + col];
        if (candidate < dp[nextMask])
        {
            dp[nextMask] = candidate;
            parentMask[nextMask] = mask;
            parentCol[nextMask] = col;
        }
    }
}

var full = maxMask - 1;
var best = dp[full];

// Reconstruct assignment row->col.
var assign = new int[n];
for (var i = 0; i < n; i++) assign[i] = -1;

var cur = full;
for (var row = n - 1; row >= 0; row--)
{
    assign[row] = parentCol[cur];
    cur = parentMask[cur];
}

// Validate cost from assignment.
var verify = 0;
var usedColsMask = 0;
for (var r = 0; r < n; r++)
{
    var c = assign[r];
    verify += cost[r * n + c];
    usedColsMask |= (1 << c);
}

var assignment = "";
for (var r = 0; r < n; r++)
{
    assignment += r + "->" + assign[r];
    if (r < n - 1) assignment += ",";
}

return $"best={best}|verify={verify}|colsMask={usedColsMask}|{assignment}";
