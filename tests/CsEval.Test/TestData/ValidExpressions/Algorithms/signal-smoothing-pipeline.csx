{
    var n = 100;
    var windowSize = 5;
    var signal = new List<double>();

    foreach (var i in 0..<n)
    {
        var noise = ((i * 7 + 13) % 37 - 18) / 10.0;
        signal.Add(sin(i * 0.1) * 5.0 + noise);
    }

    Func<List<double>, List<double>> smooth = raw =>
    {
        var result = new List<double>();
        foreach (var i in 0..<raw.Count)
        {
            var sum = 0.0;
            var count = 0;
            var lo = i - windowSize / 2;
            var hi = i + windowSize / 2;
            foreach (var j in 0..<raw.Count)
            {
                if (lo <= j <= hi)
                {
                    sum += raw[j];
                    count++;
                }
            }
            result.Add(sum / count);
        }
        return result;
    };

    var smoothed = signal |> smooth;

    var rawMean = 0.0;
    foreach (var i in 0..<n) rawMean += signal[i];
    rawMean = rawMean / n;

    var smoothMean = 0.0;
    foreach (var i in 0..<n) smoothMean += smoothed[i];
    smoothMean = smoothMean / n;

    var rawVariance = 0.0;
    foreach (var i in 0..<n)
        rawVariance += (signal[i] - rawMean) ** 2;
    rawVariance = rawVariance / n;

    var smoothVariance = 0.0;
    foreach (var i in 0..<n)
        smoothVariance += (smoothed[i] - smoothMean) ** 2;
    smoothVariance = smoothVariance / n;

    var rawStdDev = round(sqrt(rawVariance), 4);
    var smoothStdDev = round(sqrt(smoothVariance), 4);
    var energyRatio = round(smoothVariance / rawVariance, 4);
    var effective = smoothStdDev < rawStdDev;

    var result = $"rawStd={rawStdDev}|smoothStd={smoothStdDev}|";
    result += $"ratio={energyRatio}|effective={effective}|n={n}";

    return result;
}
