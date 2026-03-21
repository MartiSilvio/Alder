// Jacobi eigenvalue algorithm for symmetric matrices
// Exercises: **, pi, sin, cos, sqrt, abs, atan, ranges

var n = 3;

// Symmetric tridiagonal test matrix
var A = new double[][] {
    new double[] { 2.0, -1.0, 0.0 },
    new double[] { -1.0, 2.0, -1.0 },
    new double[] { 0.0, -1.0, 2.0 }
};

// Eigenvector matrix V = identity
var V = new double[][] {
    new double[] { 1.0, 0.0, 0.0 },
    new double[] { 0.0, 1.0, 0.0 },
    new double[] { 0.0, 0.0, 1.0 }
};

// Copy A so we can modify it
var D = new double[][] {
    new double[] { A[0][0], A[0][1], A[0][2] },
    new double[] { A[1][0], A[1][1], A[1][2] },
    new double[] { A[2][0], A[2][1], A[2][2] }
};

var maxIter = 100;
var tolerance = 1e-12;

// Sum of squares of off-diagonal elements
var offDiagNorm = () => {
    var sum = 0.0;
    foreach (var i in 0..<n)
        foreach (var j in 0..<n)
            if (i != j) sum += D[i][j] ** 2;
    return sqrt(sum);
};

// Find largest off-diagonal element
var findPivot = () => {
    var maxVal = 0.0;
    var pivI = 0;
    var pivJ = 1;
    foreach (var i in 0..<n)
    {
        foreach (var j in (i + 1)..<n)
        {
            if (abs(D[i][j]) > maxVal)
            {
                maxVal = abs(D[i][j]);
                pivI = i;
                pivJ = j;
            }
        }
    }
    return new int[] { pivI, pivJ };
};

var iterations = 0;
foreach (var iter in 0..<maxIter)
{
    if (offDiagNorm() < tolerance) break;
    iterations++;

    var pivot = findPivot();
    var p = pivot[0];
    var q = pivot[1];

    // Compute rotation angle
    var theta = 0.0;
    if (abs(D[p][p] - D[q][q]) < 1e-15)
        theta = pi / 4;
    else
        theta = 0.5 * Math.Atan2(2 * D[p][q], D[p][p] - D[q][q]);

    var cosT = cos(theta);
    var sinT = sin(theta);

    // Apply Givens rotation to D: D' = G^T * D * G
    var newDpp = cosT ** 2 * D[p][p] + 2 * sinT * cosT * D[p][q] + sinT ** 2 * D[q][q];
    var newDqq = sinT ** 2 * D[p][p] - 2 * sinT * cosT * D[p][q] + cosT ** 2 * D[q][q];
    var newDpq = 0.0;

    var rowP = new double[n];
    var rowQ = new double[n];
    foreach (var i in 0..<n)
    {
        rowP[i] = cosT * D[p][i] + sinT * D[q][i];
        rowQ[i] = -sinT * D[p][i] + cosT * D[q][i];
    }

    foreach (var i in 0..<n)
    {
        D[p][i] = rowP[i];
        D[q][i] = rowQ[i];
    }

    var colP = new double[n];
    var colQ = new double[n];
    foreach (var i in 0..<n)
    {
        colP[i] = cosT * D[i][p] + sinT * D[i][q];
        colQ[i] = -sinT * D[i][p] + cosT * D[i][q];
    }

    foreach (var i in 0..<n)
    {
        D[i][p] = colP[i];
        D[i][q] = colQ[i];
    }

    D[p][p] = newDpp;
    D[q][q] = newDqq;
    D[p][q] = newDpq;
    D[q][p] = newDpq;

    // Accumulate eigenvectors
    var vColP = new double[n];
    var vColQ = new double[n];
    foreach (var i in 0..<n)
    {
        vColP[i] = cosT * V[i][p] + sinT * V[i][q];
        vColQ[i] = -sinT * V[i][p] + cosT * V[i][q];
    }
    foreach (var i in 0..<n)
    {
        V[i][p] = vColP[i];
        V[i][q] = vColQ[i];
    }
}

// Extract eigenvalues and sort
var eigenvalues = new double[] { D[0][0], D[1][1], D[2][2] };
Array.Sort(eigenvalues);

// Known eigenvalues: 2 - sqrt(2), 2, 2 + sqrt(2)
var expected = new double[] { 2 - sqrt(2.0), 2.0, 2 + sqrt(2.0) };

var err0 = abs(eigenvalues[0] - expected[0]);
var err1 = abs(eigenvalues[1] - expected[1]);
var err2 = abs(eigenvalues[2] - expected[2]);

// Verify orthogonality of eigenvectors: V^T * V ~ I
var orthErr = 0.0;
foreach (var i in 0..<n)
{
    foreach (var j in 0..<n)
    {
        var dot = 0.0;
        foreach (var k in 0..<n)
            dot += V[k][i] * V[k][j];
        var expected_val = i == j ? 1.0 : 0.0;
        orthErr += (dot - expected_val) ** 2;
    }
}
orthErr = sqrt(orthErr);

var converged = offDiagNorm() < tolerance;
var eigenCorrect = err0 < 1e-10 and err1 < 1e-10 and err2 < 1e-10;
var orthogonal = orthErr < 1e-10;

var result = $"converged={converged}|iterations={iterations}|eigenCorrect={eigenCorrect}|";
result += $"e0={Math.Round(eigenvalues[0], 8)}|e1={Math.Round(eigenvalues[1], 8)}|e2={Math.Round(eigenvalues[2], 8)}|";
result += $"orthogonal={orthogonal}|orthErr={orthErr < 1e-10}";

return result;
