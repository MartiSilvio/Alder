var roots = new double[][] {
    new[] { 1.0, 0.0 },
    new[] { -0.5, Math.Sqrt(3.0) / 2 },
    new[] { -0.5, -Math.Sqrt(3.0) / 2 }
};

var maxIter = 50;
var tolerance = 1e-10;

var cmul = (double ar, double ai, double br, double bi) =>
    new double[] { ar * br - ai * bi, ar * bi + ai * br };

var cdiv = (double ar, double ai, double br, double bi) => {
    var denom = br * br + bi * bi;
    return new double[] { (ar * br + ai * bi) / denom, (ai * br - ar * bi) / denom };
};

var newtonStep = (double zr, double zi) => {
    var z2 = cmul(zr, zi, zr, zi);
    var z3 = cmul(z2[0], z2[1], zr, zi);
    var fr = z3[0] - 1.0;
    var fi = z3[1];
    var denom = cdiv(fr, fi, 3 * z2[0], 3 * z2[1]);
    return new double[] { zr - denom[0], zi - denom[1] };
};

var classify = (double zr, double zi) => {
    var cr = zr;
    var ci = zi;
    for (var iter = 0; iter < maxIter; iter++)
    {
        var mag2 = cr * cr + ci * ci;
        if (mag2 < 1e-20) return -1;

        var next = newtonStep(cr, ci);
        cr = next[0];
        ci = next[1];

        for (var r = 0; r < 3; r++)
        {
            var dr = cr - roots[r][0];
            var di = ci - roots[r][1];
            if (Math.Sqrt(dr * dr + di * di) < tolerance)
                return r;
        }
    }
    return -1;
};

var gridSize = 5;
var step = 3.0 / (gridSize - 1);
var convergenceCount = new int[] { 0, 0, 0, 0 };

for (var row = 0; row < gridSize; row++)
{
    for (var col = 0; col < gridSize; col++)
    {
        var x = -1.5 + col * step;
        var y = -1.5 + row * step;
        var rootIdx = classify(x, y);
        if (rootIdx >= 0 && rootIdx <= 2)
            convergenceCount[rootIdx]++;
        else
            convergenceCount[3]++;
    }
}

var total = convergenceCount[0] + convergenceCount[1] + convergenceCount[2];
var allConverged = convergenceCount[3] == 0;

var balanced = Math.Abs(convergenceCount[0] - convergenceCount[1]) <= 3
    && Math.Abs(convergenceCount[1] - convergenceCount[2]) <= 3;

var originClassify = classify(1.0, 0.0);
var negClassify = classify(-1.0, 0.0);

var result = $"total={total}|allConverged={allConverged}|balanced={balanced}|";
result += $"r0={convergenceCount[0]}|r1={convergenceCount[1]}|r2={convergenceCount[2]}|";
result += $"originRoot={originClassify}|negConverges={negClassify >= 0}";

return result;
