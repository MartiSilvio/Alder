var n = 3;

var A = new double[][] {
    new[] { 2.0, -1.0, 0.0 },
    new[] { -1.0, 2.0, -1.0 },
    new[] { 0.0, -1.0, 2.0 }
};

var V = new double[][] {
    new[] { 1.0, 0.0, 0.0 },
    new[] { 0.0, 1.0, 0.0 },
    new[] { 0.0, 0.0, 1.0 }
};

var D = new double[][] {
    new[] { A[0][0], A[0][1], A[0][2] },
    new[] { A[1][0], A[1][1], A[1][2] },
    new[] { A[2][0], A[2][1], A[2][2] }
};

var maxIter = 100;
var tolerance = 1e-12;

var offDiagNorm = () => {
    var sum = 0.0;
    for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
            if (i != j) sum += D[i][j] * D[i][j];
    return Math.Sqrt(sum);
};

var findPivot = () => {
    var maxVal = 0.0;
    var pi = 0;
    var pj = 1;
    for (var i = 0; i < n; i++)
    {
        for (var j = i + 1; j < n; j++)
        {
            if (Math.Abs(D[i][j]) > maxVal)
            {
                maxVal = Math.Abs(D[i][j]);
                pi = i;
                pj = j;
            }
        }
    }
    return new int[] { pi, pj };
};

var iterations = 0;
for (var iter = 0; iter < maxIter; iter++)
{
    if (offDiagNorm() < tolerance) break;
    iterations++;

    var pivot = findPivot();
    var p = pivot[0];
    var q = pivot[1];

    var theta = 0.0;
    if (Math.Abs(D[p][p] - D[q][q]) < 1e-15)
        theta = Math.PI / 4;
    else
        theta = 0.5 * Math.Atan2(2 * D[p][q], D[p][p] - D[q][q]);

    var c = Math.Cos(theta);
    var s = Math.Sin(theta);

    var newDpp = c * c * D[p][p] + 2 * s * c * D[p][q] + s * s * D[q][q];
    var newDqq = s * s * D[p][p] - 2 * s * c * D[p][q] + c * c * D[q][q];
    var newDpq = 0.0;

    var rowP = new double[n];
    var rowQ = new double[n];
    for (var i = 0; i < n; i++)
    {
        rowP[i] = c * D[p][i] + s * D[q][i];
        rowQ[i] = -s * D[p][i] + c * D[q][i];
    }

    for (var i = 0; i < n; i++)
    {
        D[p][i] = rowP[i];
        D[q][i] = rowQ[i];
    }

    var colP = new double[n];
    var colQ = new double[n];
    for (var i = 0; i < n; i++)
    {
        colP[i] = c * D[i][p] + s * D[i][q];
        colQ[i] = -s * D[i][p] + c * D[i][q];
    }

    for (var i = 0; i < n; i++)
    {
        D[i][p] = colP[i];
        D[i][q] = colQ[i];
    }

    D[p][p] = newDpp;
    D[q][q] = newDqq;
    D[p][q] = newDpq;
    D[q][p] = newDpq;

    var vColP = new double[n];
    var vColQ = new double[n];
    for (var i = 0; i < n; i++)
    {
        vColP[i] = c * V[i][p] + s * V[i][q];
        vColQ[i] = -s * V[i][p] + c * V[i][q];
    }
    for (var i = 0; i < n; i++)
    {
        V[i][p] = vColP[i];
        V[i][q] = vColQ[i];
    }
}

var eigenvalues = new double[] { D[0][0], D[1][1], D[2][2] };
Array.Sort(eigenvalues);

var expected = new double[] { 2 - Math.Sqrt(2.0), 2.0, 2 + Math.Sqrt(2.0) };

var err0 = Math.Abs(eigenvalues[0] - expected[0]);
var err1 = Math.Abs(eigenvalues[1] - expected[1]);
var err2 = Math.Abs(eigenvalues[2] - expected[2]);

var orthErr = 0.0;
for (var i = 0; i < n; i++)
{
    for (var j = 0; j < n; j++)
    {
        var dot = 0.0;
        for (var k = 0; k < n; k++)
            dot += V[k][i] * V[k][j];
        var expected_val = i == j ? 1.0 : 0.0;
        orthErr += (dot - expected_val) * (dot - expected_val);
    }
}
orthErr = Math.Sqrt(orthErr);

var converged = offDiagNorm() < tolerance;
var eigenCorrect = err0 < 1e-10 && err1 < 1e-10 && err2 < 1e-10;
var orthogonal = orthErr < 1e-10;

var result = $"converged={converged}|iterations={iterations}|eigenCorrect={eigenCorrect}|";
result += $"e0={Math.Round(eigenvalues[0], 8)}|e1={Math.Round(eigenvalues[1], 8)}|e2={Math.Round(eigenvalues[2], 8)}|";
result += $"orthogonal={orthogonal}|orthErr={orthErr < 1e-10}";

return result;
