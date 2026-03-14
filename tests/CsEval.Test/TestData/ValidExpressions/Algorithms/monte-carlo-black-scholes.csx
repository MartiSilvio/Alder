// Monte Carlo Black-Scholes option pricing with Box-Muller transform
// Exercises: **, implicit mult, pi, sqrt, abs, log, exp, ranges, between

// Black-Scholes parameters
var S0 = 100.0;     // Initial stock price
var K = 105.0;      // Strike price
var r = 0.05;       // Risk-free rate
var sigma_v = 0.2;  // Volatility
var T = 1.0;        // Time to expiry (years)
var nPaths = 2000;
var nSteps = 50;
var dt = T / nSteps;

// Linear congruential generator (deterministic for reproducibility)
var seed = 42L;
var a = 1103515245L;
var c = 12345L;
var m = (long)(2.0 ** 31);

var nextRand = () => {
    seed = (a * seed + c) % m;
    return (double)seed / m;
};

// Box-Muller transform: generate standard normal from uniform
var boxMuller = () => {
    var u1 = nextRand();
    var u2 = nextRand();
    while (u1 < 1e-10) u1 = nextRand();
    return sqrt(-2 * log(u1)) * cos(2 * pi * u2);
};

// Simulate one GBM path and return final price
// S(t+dt) = S(t) * exp((r - σ²/2)dt + σ√dt * Z)
var drift = (r - 0.5 * sigma_v ** 2) * dt;
var diffusion = sigma_v * sqrt(dt);

var simulatePath = () => {
    var S = S0;
    foreach (var step in 0..<nSteps)
    {
        var Z = boxMuller();
        S *= exp(drift + diffusion * Z);
    }
    return S;
};

// Price European call and put
var callPayoffSum = 0.0;
var putPayoffSum = 0.0;
var callPayoffSqSum = 0.0;
var putPayoffSqSum = 0.0;

foreach (var path in 0..<nPaths)
{
    var ST = simulatePath();
    var callPayoff = ST > K ? ST - K : 0.0;
    var putPayoff = ST < K ? K - ST : 0.0;

    callPayoffSum += callPayoff;
    putPayoffSum += putPayoff;
    callPayoffSqSum += callPayoff ** 2;
    putPayoffSqSum += putPayoff ** 2;
}

var discount = exp(-r * T);
var callPrice = discount * callPayoffSum / nPaths;
var putPrice = discount * putPayoffSum / nPaths;

// Standard error of Monte Carlo estimate
var callVar = callPayoffSqSum / nPaths - (callPayoffSum / nPaths) ** 2;
var callSE = discount * sqrt(callVar / nPaths);

// Put-call parity: C - P = S0 - K*exp(-rT)
var parityLHS = callPrice - putPrice;
var parityRHS = S0 - K * discount;
var parityErr = abs(parityLHS - parityRHS);

// Analytical Black-Scholes for comparison
// d1 = (ln(S0/K) + (r + σ²/2)T) / (σ√T)
var d1 = (log(S0 / K) + (r + 0.5 * sigma_v ** 2) * T) / (sigma_v * sqrt(T));
var d2 = d1 - sigma_v * sqrt(T);

// Approximate Φ(x) using Abramowitz & Stegun
var normCdf = (double x) => {
    var t = 1.0 / (1.0 + 0.2316419 * abs(x));
    var d = 0.3989422804014327;
    var poly = t * (0.319381530 + t * (-0.356563782 + t * (1.781477937 + t * (-1.821255978 + 1.330274429 * t))));
    var cdf = 1.0 - d * exp(-0.5 * x ** 2) * poly;
    return x >= 0 ? cdf : 1.0 - cdf;
};

var bsCallPrice = S0 * normCdf(d1) - K * discount * normCdf(d2);
var bsPutPrice = K * discount * normCdf(-d2) - S0 * normCdf(-d1);

// Monte Carlo should be within ~3 standard errors of analytical
var callWithinBounds = abs(callPrice - bsCallPrice) < 3 * callSE + 1.0;

// Verify put-call parity holds for both MC and analytical
var parityOk = parityErr < 1.0;
var bsParityErr = abs((bsCallPrice - bsPutPrice) - parityRHS);
var bsParityOk = bsParityErr < 1e-10;

// Prices should be positive and in reasonable range
var callPositive = callPrice > 0;
var putPositive = putPrice > 0;
var callReasonable = callPrice between 1 and 30;
var putReasonable = putPrice between 1 and 30;

var result = $"callPositive={callPositive}|putPositive={putPositive}|";
result += $"parityOk={parityOk}|bsParityOk={bsParityOk}|";
result += $"callReasonable={callReasonable}|putReasonable={putReasonable}|";
result += $"callWithinBounds={callWithinBounds}|";
result += $"bsCall={Math.Round(bsCallPrice, 2)}|bsPut={Math.Round(bsPutPrice, 2)}";

return result;
