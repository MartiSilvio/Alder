// Longest Common Subsequence with reconstruction
// Exercises: 2D DP, string indexing, new int[n], backtrack reconstruction

var strA = "AGGTAB";
var strB = "GXTXAYB";
var lenA = strA.Length;
var lenB = strB.Length;

// DP table: (lenA+1) x (lenB+1)
var dpCols = lenB + 1;
var dp = new int[((lenA + 1) * (lenB + 1))];

// Fill DP table
for (var i = 1; i <= lenA; i++)
{
    for (var j = 1; j <= lenB; j++)
    {
        if (strA[i - 1] == strB[j - 1])
        {
            dp[i * dpCols + j] = dp[(i - 1) * dpCols + (j - 1)] + 1;
        }
        else
        {
            var up = dp[(i - 1) * dpCols + j];
            var left = dp[i * dpCols + (j - 1)];
            dp[i * dpCols + j] = up > left ? up : left;
        }
    }
}

var lcsLength = dp[lenA * dpCols + lenB];

// Backtrack to reconstruct the LCS
var lcsChars = new char[lcsLength];
var lcsIdx = lcsLength - 1;
var bi = lenA;
var bj = lenB;

while (bi > 0 && bj > 0)
{
    if (strA[bi - 1] == strB[bj - 1])
    {
        lcsChars[lcsIdx] = strA[bi - 1];
        lcsIdx--;
        bi--;
        bj--;
    }
    else if (dp[(bi - 1) * dpCols + bj] > dp[bi * dpCols + (bj - 1)])
    {
        bi--;
    }
    else
    {
        bj--;
    }
}

// Build LCS string
var lcsStr = "";
for (var i = 0; i < lcsLength; i++)
{
    lcsStr += lcsChars[i].ToString();
}

// Second test: longer strings
var strC = "ABCBDAB";
var strD = "BDCABA";
var lenC = strC.Length;
var lenD = strD.Length;

var dp2Cols = lenD + 1;
var dp2 = new int[((lenC + 1) * (lenD + 1))];

for (var i = 1; i <= lenC; i++)
{
    for (var j = 1; j <= lenD; j++)
    {
        if (strC[i - 1] == strD[j - 1])
            dp2[i * dp2Cols + j] = dp2[(i - 1) * dp2Cols + (j - 1)] + 1;
        else
        {
            var u = dp2[(i - 1) * dp2Cols + j];
            var l = dp2[i * dp2Cols + (j - 1)];
            dp2[i * dp2Cols + j] = u > l ? u : l;
        }
    }
}

var lcs2Length = dp2[lenC * dp2Cols + lenD];

// Verify LCS is a subsequence of both strings
var isSubA = true;
var isSubB = true;
var posA = 0;
var posB = 0;
for (var k = 0; k < lcsLength; k++)
{
    var found = false;
    while (posA < lenA) { if (strA[posA] == lcsChars[k]) { posA++; found = true; break; } posA++; }
    if (!found) isSubA = false;

    found = false;
    while (posB < lenB) { if (strB[posB] == lcsChars[k]) { posB++; found = true; break; } posB++; }
    if (!found) isSubB = false;
}

var result = $"lcs1Len={lcsLength}|lcs1={lcsStr}|";
result += $"validSubA={isSubA}|validSubB={isSubB}|";
result += $"lcs2Len={lcs2Length}|";
result += $"dpSize1={lenA + 1}x{lenB + 1}|dpSize2={lenC + 1}x{lenD + 1}";

return result;
