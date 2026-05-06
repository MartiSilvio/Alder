var S0 = 100.0;
var K = 105.0;
var r = 0.05;
var sigma_v = 0.2;
var T = 1.0;
var nPaths = 2000;
var nSteps = 50;
var dt = T / nSteps;

var seed = 42L;
var a = 1103515245L;
var c = 12345L;
var m = (long)Math.Pow(2, 31);

var nextRand = () => {
    seed = (a * seed + c) % m;
    return (double)seed / m;
};

var boxMuller = () => {
    var u1 = nextRand();
    var u2 = nextRand();
    while (u1 < 1e-10) u1 = nextRand();
    return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
};

var drift = (r - 0.5 * sigma_v * sigma_v) * dt;
var diffusion = sigma_v * Math.Sqrt(dt);

var simulatePath = () => {
    var S = S0;
    for (var step = 0; step < nSteps; step++)
    {
        var Z = boxMuller();
        S *= Math.Exp(drift + diffusion * Z);
    }
    return S;
};

var callPayoffSum = 0.0;
var putPayoffSum = 0.0;
var callPayoffSqSum = 0.0;
var putPayoffSqSum = 0.0;

for (var path = 0; path < nPaths; path++)
{
    var ST = simulatePath();
    var callPayoff = ST > K ? ST - K : 0.0;
    var putPayoff = ST < K ? K - ST : 0.0;

    callPayoffSum += callPayoff;
    putPayoffSum += putPayoff;
    callPayoffSqSum += callPayoff * callPayoff;
    putPayoffSqSum += putPayoff * putPayoff;
}

var discount = Math.Exp(-r * T);
var callPrice = discount * callPayoffSum / nPaths;
var putPrice = discount * putPayoffSum / nPaths;

var callVar = callPayoffSqSum / nPaths - Math.Pow(callPayoffSum / nPaths, 2);
var callSE = discount * Math.Sqrt(callVar / nPaths);

var parityLHS = callPrice - putPrice;
var parityRHS = S0 - K * discount;
var parityErr = Math.Abs(parityLHS - parityRHS);

var d1 = (Math.Log(S0 / K) + (r + 0.5 * sigma_v * sigma_v) * T) / (sigma_v * Math.Sqrt(T));
var d2 = d1 - sigma_v * Math.Sqrt(T);

var normCdf = (double x) => {
    var t = 1.0 / (1.0 + 0.2316419 * Math.Abs(x));
    var d = 0.3989422804014327;
    var poly = t * (0.319381530 + t * (-0.356563782 + t * (1.781477937 + t * (-1.821255978 + 1.330274429 * t))));
    var cdf = 1.0 - d * Math.Exp(-0.5 * x * x) * poly;
    return x >= 0 ? cdf : 1.0 - cdf;
};

var bsCallPrice = S0 * normCdf(d1) - K * discount * normCdf(d2);
var bsPutPrice = K * discount * normCdf(-d2) - S0 * normCdf(-d1);

var callWithinBounds = Math.Abs(callPrice - bsCallPrice) < 3 * callSE + 1.0;

var parityOk = parityErr < 1.0;
var bsParityErr = Math.Abs((bsCallPrice - bsPutPrice) - parityRHS);
var bsParityOk = bsParityErr < 1e-10;

var callPositive = callPrice > 0;
var putPositive = putPrice > 0;
var callReasonable = callPrice >= 1 && callPrice <= 30;
var putReasonable = putPrice >= 1 && putPrice <= 30;

var result = $"callPositive={callPositive}|putPositive={putPositive}|";
result += $"parityOk={parityOk}|bsParityOk={bsParityOk}|";
result += $"callReasonable={callReasonable}|putReasonable={putReasonable}|";
result += $"callWithinBounds={callWithinBounds}|";
result += $"bsCall={Math.Round(bsCallPrice, 2)}|bsPut={Math.Round(bsPutPrice, 2)}";

return result;
