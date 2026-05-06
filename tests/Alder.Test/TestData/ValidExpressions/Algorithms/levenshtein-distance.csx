// Levenshtein edit distance with operation reconstruction
// Exercises: 2D DP flat array, Math.Min, string indexing, operation tracking, backtracking

var src = "kitten";
var tgt = "sitting";
var srcLen = src.Length;
var tgtLen = tgt.Length;

// DP table: (srcLen+1) x (tgtLen+1)
var dpCols = tgtLen + 1;
var dp = new int[((srcLen + 1) * (tgtLen + 1))];

// Operation tracking: 0=none, 1=insert, 2=delete, 3=substitute, 4=match
var ops = new int[((srcLen + 1) * (tgtLen + 1))];

// Initialize base cases
for (var i = 0; i <= srcLen; i++)
{
    dp[i * dpCols + 0] = i;
    if (i > 0) ops[i * dpCols + 0] = 2; // delete
}
for (var j = 0; j <= tgtLen; j++)
{
    dp[0 * dpCols + j] = j;
    if (j > 0) ops[0 * dpCols + j] = 1; // insert
}

// Fill DP table
for (var i = 1; i <= srcLen; i++)
{
    for (var j = 1; j <= tgtLen; j++)
    {
        var cost = src[i - 1] == tgt[j - 1] ? 0 : 1;
        var del = dp[(i - 1) * dpCols + j] + 1;
        var ins = dp[i * dpCols + (j - 1)] + 1;
        var sub = dp[(i - 1) * dpCols + (j - 1)] + cost;

        // Find minimum
        var min = del;
        var op = 2; // delete
        if (ins < min) { min = ins; op = 1; } // insert
        if (sub < min) { min = sub; op = cost == 0 ? 4 : 3; } // match or substitute
        if (sub == min && cost == 0) { op = 4; } // prefer match

        dp[i * dpCols + j] = min;
        ops[i * dpCols + j] = op;
    }
}

var distance = dp[srcLen * dpCols + tgtLen];

// Backtrack to reconstruct edit operations
var editOps = "";
var matchCount = 0;
var insertCount = 0;
var deleteCount = 0;
var substituteCount = 0;
var bi = srcLen;
var bj = tgtLen;

while (bi > 0 || bj > 0)
{
    var op = ops[bi * dpCols + bj];
    if (op == 4) { editOps = "M" + editOps; matchCount++; bi--; bj--; }
    else if (op == 3) { editOps = "S" + editOps; substituteCount++; bi--; bj--; }
    else if (op == 2) { editOps = "D" + editOps; deleteCount++; bi--; }
    else { editOps = "I" + editOps; insertCount++; bj--; }
}

// Second pair: "saturday" -> "sunday"
var src2 = "saturday";
var tgt2 = "sunday";
var s2Len = src2.Length;
var t2Len = tgt2.Length;

var dp2Cols = t2Len + 1;
var dp2 = new int[((s2Len + 1) * (t2Len + 1))];

for (var i = 0; i <= s2Len; i++) dp2[i * dp2Cols + 0] = i;
for (var j = 0; j <= t2Len; j++) dp2[0 * dp2Cols + j] = j;

for (var i = 1; i <= s2Len; i++)
{
    for (var j = 1; j <= t2Len; j++)
    {
        var cost = src2[i - 1] == tgt2[j - 1] ? 0 : 1;
        var d = dp2[(i - 1) * dp2Cols + j] + 1;
        var n = dp2[i * dp2Cols + (j - 1)] + 1;
        var s = dp2[(i - 1) * dp2Cols + (j - 1)] + cost;
        var m = d;
        if (n < m) m = n;
        if (s < m) m = s;
        dp2[i * dp2Cols + j] = m;
    }
}

var distance2 = dp2[s2Len * dp2Cols + t2Len];

var result = $"dist1={distance}|ops={editOps}|";
result += $"matches={matchCount}|inserts={insertCount}|deletes={deleteCount}|subs={substituteCount}|";
result += $"dist2={distance2}|totalOps={editOps.Length}";

return result;
