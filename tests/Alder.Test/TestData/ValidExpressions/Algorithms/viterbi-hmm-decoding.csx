// Viterbi decoding for a small HMM in log-space.

var states = 3;
var obsKinds = 4;

// Transition probabilities T[from,to]
var T = new[] {
    0.60, 0.30, 0.10,
    0.20, 0.50, 0.30,
    0.10, 0.30, 0.60
};

// Emission probabilities E[state,obs]
var E = new[] {
    0.50, 0.30, 0.15, 0.05,
    0.10, 0.40, 0.40, 0.10,
    0.05, 0.20, 0.35, 0.40
};

var pi = new[] { 0.7, 0.2, 0.1 };
var observations = new[] { 0, 1, 2, 3, 2, 1, 0, 1, 3, 2, 2, 1, 0 };
var tLen = observations.Length;

var logT = new double[T.Length];
var logE = new double[E.Length];
var logPi = new double[pi.Length];
for (var i = 0; i < T.Length; i++) logT[i] = Math.Log(T[i]);
for (var i = 0; i < E.Length; i++) logE[i] = Math.Log(E[i]);
for (var i = 0; i < pi.Length; i++) logPi[i] = Math.Log(pi[i]);

var dp = new double[tLen * states];
var prev = new int[tLen * states];

for (var s = 0; s < states; s++)
{
    dp[s] = logPi[s] + logE[s * obsKinds + observations[0]];
    prev[s] = -1;
}

for (var t = 1; t < tLen; t++)
{
    var o = observations[t];
    for (var s = 0; s < states; s++)
    {
        var best = -1e300;
        var bestPrev = -1;

        for (var ps = 0; ps < states; ps++)
        {
            var candidate = dp[(t - 1) * states + ps] + logT[ps * states + s];
            if (candidate > best)
            {
                best = candidate;
                bestPrev = ps;
            }
        }

        dp[t * states + s] = best + logE[s * obsKinds + o];
        prev[t * states + s] = bestPrev;
    }
}

// Find best final state
var bestFinal = 0;
for (var s = 1; s < states; s++)
{
    if (dp[(tLen - 1) * states + s] > dp[(tLen - 1) * states + bestFinal]) bestFinal = s;
}

// Backtrack path
var path = new int[tLen];
var cur = bestFinal;
for (var t = tLen - 1; t >= 0; t--)
{
    path[t] = cur;
    cur = prev[t * states + cur];
}

// Path histogram and checksum
var hist = new int[states];
for (var i = 0; i < hist.Length; i++) hist[i] = 0;
var checksum = 19;
for (var t = 0; t < tLen; t++)
{
    hist[path[t]]++;
    checksum = checksum * 29 + path[t] * (t + 7);
}

var pathStr = "";
for (var t = 0; t < tLen; t++)
{
    pathStr += path[t];
    if (t < tLen - 1) pathStr += ">";
}

return $"final={bestFinal}|logp={Math.Round(dp[(tLen - 1) * states + bestFinal], 6):0.000000}|hist={hist[0]},{hist[1]},{hist[2]}|chk={checksum}|path={pathStr}";
