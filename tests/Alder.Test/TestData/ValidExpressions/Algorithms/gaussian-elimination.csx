// Gaussian elimination with partial pivoting on a 5x5 system
// Exercises: double arithmetic, Math.Abs, new double[n], row swapping, back-substitution

var n = 5;
var cols = n + 1;
// Augmented matrix [A|b] — system with known solution x = {1, 2, 3, 4, 5}
var aug = new double[30]; // 5 rows x 6 cols
var origA = new[] {
    2.0,  1.0, -1.0,  3.0,  2.0,
    4.0,  5.0, -3.0,  6.0,  1.0,
   -2.0,  3.0,  1.0,  2.0,  4.0,
    1.0, -1.0,  4.0, -1.0,  3.0,
    3.0,  2.0,  2.0,  5.0, -2.0
};
var origB = new[] { 30.0, 49.0, 34.0, 18.0, 17.0 };

// Copy into augmented matrix
for (var i = 0; i < n; i++)
{
    for (var j = 0; j < n; j++)
        aug[i * cols + j] = origA[i * n + j];
    aug[i * cols + n] = origB[i];
}

var swapCount = 0;

// Forward elimination with partial pivoting
for (var col = 0; col < n; col++)
{
    // Find pivot
    var maxVal = Math.Abs(aug[col * cols + col]);
    var maxRow = col;
    for (var row = col + 1; row < n; row++)
    {
        var absVal = Math.Abs(aug[row * cols + col]);
        if (absVal > maxVal)
        {
            maxVal = absVal;
            maxRow = row;
        }
    }

    // Swap rows
    if (maxRow != col)
    {
        swapCount++;
        for (var j = 0; j < cols; j++)
        {
            var temp = aug[col * cols + j];
            aug[col * cols + j] = aug[maxRow * cols + j];
            aug[maxRow * cols + j] = temp;
        }
    }

    // Eliminate below
    for (var row = col + 1; row < n; row++)
    {
        var factor = aug[row * cols + col] / aug[col * cols + col];
        for (var j = col; j < cols; j++)
        {
            aug[row * cols + j] = aug[row * cols + j] - factor * aug[col * cols + j];
        }
    }
}

// Back substitution
var x = new double[5];
for (var i = n - 1; i >= 0; i--)
{
    var sum = 0.0;
    for (var j = i + 1; j < n; j++)
        sum = sum + aug[i * cols + j] * x[j];
    x[i] = (aug[i * cols + n] - sum) / aug[i * cols + i];
}

// Round solutions
var roundedSum = 0;
var allInteger = true;
for (var i = 0; i < n; i++)
{
    var rounded = (int)(x[i] + (x[i] >= 0 ? 0.5 : -0.5));
    var diff = Math.Abs(x[i] - rounded);
    if (diff > 0.0001) allInteger = false;
    roundedSum = roundedSum + rounded * (i + 1);
}

// Verify residual
var maxResidual = 0.0;
for (var i = 0; i < n; i++)
{
    var rowSum = 0.0;
    for (var j = 0; j < n; j++)
        rowSum = rowSum + origA[i * n + j] * x[j];
    var residual = Math.Abs(rowSum - origB[i]);
    if (residual > maxResidual) maxResidual = residual;
}

// Check upper triangular form
var isUpperTriangular = true;
for (var i = 1; i < n; i++)
{
    for (var j = 0; j < i; j++)
    {
        if (Math.Abs(aug[i * cols + j]) > 0.0001) isUpperTriangular = false;
    }
}

// Determinant from diagonal
var det = 1.0;
for (var i = 0; i < n; i++) det = det * aug[i * cols + i];
if (swapCount % 2 == 1) det = -det;

var result = $"allInteger={allInteger}|weightedSum={roundedSum}|";
result += $"swaps={swapCount}|upperTri={isUpperTriangular}|";
result += $"residual={(maxResidual < 0.0001 ? "zero" : "nonzero")}|";
result += $"x0={(int)(x[0] + 0.5)},x1={(int)(x[1] + 0.5)},x2={(int)(x[2] + 0.5)},x3={(int)(x[3] + 0.5)},x4={(int)(x[4] + 0.5)}";

return result;
