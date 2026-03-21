// Advanced C# syntax stress:
// - tuple deconstruction
// - switch expressions with relational patterns
// - try/catch when + finally
// - lock
// - null-coalescing assignment
//
// Algorithm:
// - deterministic stream generation
// - rolling z-score anomaly detector (Welford online variance)
// - severity bucketing and segment aggregation

var count = 240;
var series = new double[count];
var seed = 987654321;

// Deterministic pseudo-random stream with trend + periodicity + controlled spikes.
for (var i = 0; i < count; i++)
{
    seed = (seed * 1664525 + 1013904223) & 2147483647;
    var noise = ((seed % 2001) - 1000) / 500.0; // ~[-2,2]
    var periodic = Math.Sin(i / 7.0) * 4.0 + Math.Cos(i / 17.0) * 2.5;
    var trend = i * 0.03;
    var spike = (i % 53 == 0 || i % 79 == 0) ? 11.0 : 0.0;
    series[i] = trend + periodic + noise + spike;
}

var mean = 0.0;
var m2 = 0.0;
var n = 0;

var anomalies = 0;
var severe = 0;
var warn = 0;
var mild = 0;
var stable = 0;
var longestRun = 0;
var curRun = 0;
var weightedScore = 0;

// Segment stats over 6 buckets
var segCount = 6;
var segSize = count / segCount;
var segAnom = new int[segCount];
for (var s = 0; s < segCount; s++) segAnom[s] = 0;

var guardedErrors = 0;
var finalFlag = false;

try
{
    for (var i = 0; i < count; i++)
    {
        var x = series[i];
        n++;
        var delta = x - mean;
        mean += delta / n;
        var delta2 = x - mean;
        m2 += delta * delta2;

        var variance = n > 1 ? (m2 / (n - 1)) : 0.0;
        var std = variance > 0 ? Math.Sqrt(variance) : 0.0;

        // Before enough warmup, mark stable.
        if (n < 12 || std < 1e-9)
        {
            stable++;
            curRun = 0;
            continue;
        }

        var z = Math.Abs((x - mean) / std);

        var level = z switch
        {
            >= 3.5 => 3,
            >= 2.5 => 2,
            >= 1.8 => 1,
            _ => 0
        };

        switch (level)
        {
            case 3:
                severe++;
                anomalies++;
                curRun++;
                weightedScore += 9;
                break;
            case 2:
                warn++;
                anomalies++;
                curRun++;
                weightedScore += 5;
                break;
            case 1:
                mild++;
                anomalies++;
                curRun++;
                weightedScore += 2;
                break;
            default:
                stable++;
                curRun = 0;
                weightedScore += 0;
                break;
        }

        if (curRun > longestRun) longestRun = curRun;

        var seg = i / segSize;
        if (seg >= segCount) seg = segCount - 1;
        if (level > 0) segAnom[seg]++;

        // Intentional guarded math edge case to exercise catch when.
        try
        {
            if (i % 97 == 0 && i > 0)
            {
                var forced = 0;
                var _ = 100 / forced;
            }
        }
        catch (DivideByZeroException ex) when (ex.Message.Length > 0)
        {
            guardedErrors++;
            weightedScore += 1;
        }
    }
}
finally
{
    finalFlag = true;
}

// Deconstruction, lock, ??=
object lockObj = new object();
string? tier = null;
lock (lockObj)
{
    var (sev, wrn, mld) = (severe, warn, mild);
    tier ??= (sev + wrn + mld) switch
    {
        > 40 => "red",
        > 20 => "amber",
        _ => "green"
    };
}

// Compact digest (without using/IO surface)
var segSig = "";
for (var s = 0; s < segCount; s++)
{
    segSig += segAnom[s].ToString();
    if (s < segCount - 1) segSig += "-";
}

var checksum = 13;
for (var i = 0; i < count; i += 3)
{
    var q = (int)Math.Round(series[i] * 10.0);
    checksum = checksum * 31 + q * (i + 1);
}

var bytes = segCount;
return $"anom={anomalies}|sev={severe}|warn={warn}|mild={mild}|stable={stable}|run={longestRun}|score={weightedScore}|tier={tier}|seg={segSig}|guard={guardedErrors}|final={finalFlag}|bytes={bytes}|chk={checksum}";
