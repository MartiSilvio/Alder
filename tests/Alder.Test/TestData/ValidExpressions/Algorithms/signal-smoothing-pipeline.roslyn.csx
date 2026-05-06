{
    var n = 100;
    var windowSize = 5;
    var signal = new List<double>();

    for (var i = 0; i < n; i++)
    {
        var noise = ((i * 7 + 13) % 37 - 18) / 10.0;
        signal.Add(Math.Sin(i * 0.1) * 5.0 + noise);
    }

    Func<List<double>, List<double>> smooth = raw =>
    {
        var result = new List<double>();
        for (var i = 0; i < raw.Count; i++)
        {
            var sum = 0.0;
            var count = 0;
            var lo = i - windowSize / 2;
            var hi = i + windowSize / 2;
            for (var j = 0; j < raw.Count; j++)
            {
                if (j >= lo && j <= hi)
                {
                    sum += raw[j];
                    count++;
                }
            }
            result.Add(sum / count);
        }
        return result;
    };

    var smoothed = smooth(signal);

    var rawMean = 0.0;
    for (var i = 0; i < n; i++) rawMean += signal[i];
    rawMean = rawMean / n;

    var smoothMean = 0.0;
    for (var i = 0; i < n; i++) smoothMean += smoothed[i];
    smoothMean = smoothMean / n;

    var rawVariance = 0.0;
    for (var i = 0; i < n; i++)
        rawVariance += Math.Pow(signal[i] - rawMean, 2);
    rawVariance = rawVariance / n;

    var smoothVariance = 0.0;
    for (var i = 0; i < n; i++)
        smoothVariance += Math.Pow(smoothed[i] - smoothMean, 2);
    smoothVariance = smoothVariance / n;

    var rawStdDev = Math.Round(Math.Sqrt(rawVariance), 4);
    var smoothStdDev = Math.Round(Math.Sqrt(smoothVariance), 4);
    var energyRatio = Math.Round(smoothVariance / rawVariance, 4);
    var effective = smoothStdDev < rawStdDev;

    var result = $"rawStd={rawStdDev}|smoothStd={smoothStdDev}|";
    result += $"ratio={energyRatio}|effective={effective}|n={n}";

    return result;
}
