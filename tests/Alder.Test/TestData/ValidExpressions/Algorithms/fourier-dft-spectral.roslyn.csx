var N = 16;

var signal = new double[N];
for (var i = 0; i < N; i++)
{
    var t = (double)i / N;
    signal[i] = 3 * Math.Sin(2 * Math.PI * t) + 1.5 * Math.Cos(6 * Math.PI * t) + 0.5 * Math.Sin(10 * Math.PI * t);
}

var realPart = new double[N];
var imagPart = new double[N];

for (var k = 0; k < N; k++)
{
    var sumR = 0.0;
    var sumI = 0.0;
    for (var n = 0; n < N; n++)
    {
        var angle = -2 * Math.PI * k * n / N;
        sumR += signal[n] * Math.Cos(angle);
        sumI += signal[n] * Math.Sin(angle);
    }
    realPart[k] = sumR;
    imagPart[k] = sumI;
}

var magnitudes = new double[N];
for (var k = 0; k < N; k++)
{
    magnitudes[k] = Math.Sqrt(realPart[k] * realPart[k] + imagPart[k] * imagPart[k]) / N;
}

var reconstructed = new double[N];
for (var n = 0; n < N; n++)
{
    var sumR = 0.0;
    for (var k = 0; k < N; k++)
    {
        var angle = 2 * Math.PI * k * n / N;
        sumR += realPart[k] * Math.Cos(angle) - imagPart[k] * Math.Sin(angle);
    }
    reconstructed[n] = sumR / N;
}

var maxReconErr = 0.0;
for (var i = 0; i < N; i++)
{
    var err = Math.Abs(reconstructed[i] - signal[i]);
    if (err > maxReconErr) maxReconErr = err;
}

var timeEnergy = 0.0;
for (var i = 0; i < N; i++)
    timeEnergy += signal[i] * signal[i];

var freqEnergy = 0.0;
for (var k = 0; k < N; k++)
    freqEnergy += realPart[k] * realPart[k] + imagPart[k] * imagPart[k];
freqEnergy /= N;

var parsevalErr = Math.Abs(timeEnergy - freqEnergy);

var peaks = new int[3];
var peakMags = new double[3];

for (var k = 1; k < N / 2; k++)
{
    var mag = 2 * magnitudes[k];
    if (mag > peakMags[0])
    {
        peakMags[2] = peakMags[1]; peaks[2] = peaks[1];
        peakMags[1] = peakMags[0]; peaks[1] = peaks[0];
        peakMags[0] = mag; peaks[0] = k;
    }
    else if (mag > peakMags[1])
    {
        peakMags[2] = peakMags[1]; peaks[2] = peaks[1];
        peakMags[1] = mag; peaks[1] = k;
    }
    else if (mag > peakMags[2])
    {
        peakMags[2] = mag; peaks[2] = k;
    }
}

Array.Sort(peaks);

var peaksCorrect = peaks[0] == 1 && peaks[1] == 3 && peaks[2] == 5;
var reconstructOk = maxReconErr < 1e-10;
var parsevalOk = parsevalErr < 1e-8;

var amp1 = Math.Round(peakMags[0], 1);
var amp3 = Math.Round(peakMags[1], 1);
var amp5 = Math.Round(peakMags[2], 1);

var result = $"peaksCorrect={peaksCorrect}|bins={peaks[0]},{peaks[1]},{peaks[2]}|";
result += $"amp1={amp1}|amp3={amp3}|amp5={amp5}|";
result += $"reconstructOk={reconstructOk}|parsevalOk={parsevalOk}";

return result;
