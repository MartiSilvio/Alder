var roots = new double[][] {
    new double[] { 1.0, 0.0 },
    new double[] { -0.5, sqrt(3.0) / 2 },
    new double[] { -0.5, -sqrt(3.0) / 2 }
};

var maxIter = 50;
var tolerance = 1e-10;

var cmul = (double ar, double ai, double br, double bi) =>
    new double[] { ar * br - ai * bi, ar * bi + ai * br };

var cdiv = (double ar, double ai, double br, double bi) => {
    var denom = br ** 2 + bi ** 2;
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
    foreach (var iter in 0..<maxIter)
    {
        var mag2 = cr ** 2 + ci ** 2;
        if (mag2 < 1e-20) return -1;

        var next = newtonStep(cr, ci);
        cr = next[0];
        ci = next[1];

        foreach (var r in 0..<3)
        {
            var dr = cr - roots[r][0];
            var di = ci - roots[r][1];
            if (sqrt(dr ** 2 + di ** 2) < tolerance)
                return r;
        }
    }
    return -1;
};

var gridSize = 5;
var step = 3.0 / (gridSize - 1);
var convergenceCount = new int[] { 0, 0, 0, 0 };

foreach (var row in 0..<gridSize)
{
    foreach (var col in 0..<gridSize)
    {
        var x = -1.5 + col * step;
        var y = -1.5 + row * step;
        var rootIdx = classify(x, y);
        if (0 <= rootIdx and rootIdx <= 2)
            convergenceCount[rootIdx]++;
        else
            convergenceCount[3]++;
    }
}

var total = convergenceCount[0] + convergenceCount[1] + convergenceCount[2];
var allConverged = convergenceCount[3] == 0;

var balanced = abs(convergenceCount[0] - convergenceCount[1]) <= 3
    and abs(convergenceCount[1] - convergenceCount[2]) <= 3;

var originClassify = classify(1.0, 0.0);
var negClassify = classify(-1.0, 0.0);

var result = $"total={total}|allConverged={allConverged}|balanced={balanced}|";
result += $"r0={convergenceCount[0]}|r1={convergenceCount[1]}|r2={convergenceCount[2]}|";
result += $"originRoot={originClassify}|negConverges={negClassify >= 0}";

return result;
