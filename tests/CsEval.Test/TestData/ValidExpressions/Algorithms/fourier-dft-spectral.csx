var N = 16;

// Compose signal: 3*sin(2πt) + 1.5*cos(6πt) + 0.5*sin(10πt)
var signal = new double[N];
foreach (var i in 0..<N)
{
    var t = (double)i / N;
    signal[i] = 3 * sin(2pi * t) + 1.5 * cos(6pi * t) + 0.5 * sin(10pi * t);
}

// DFT: X[k] = sum x[n] * e^{-j2πkn/N}
var realPart = new double[N];
var imagPart = new double[N];

foreach (var k in 0..<N)
{
    var sumR = 0.0;
    var sumI = 0.0;
    foreach (var n in 0..<N)
    {
        var angle = -2 * pi * k * n / N;
        sumR += signal[n] * cos(angle);
        sumI += signal[n] * sin(angle);
    }
    realPart[k] = sumR;
    imagPart[k] = sumI;
}

// Magnitude spectrum
var magnitudes = new double[N];
foreach (var k in 0..<N)
{
    magnitudes[k] = sqrt(realPart[k] ** 2 + imagPart[k] ** 2) / N;
}

// Inverse DFT to verify reconstruction
var reconstructed = new double[N];
foreach (var n in 0..<N)
{
    var sumR = 0.0;
    foreach (var k in 0..<N)
    {
        var angle = 2pi * k * n / N;
        sumR += realPart[k] * cos(angle) - imagPart[k] * sin(angle);
    }
    reconstructed[n] = sumR / N;
}

// Reconstruction error
var maxReconErr = 0.0;
foreach (var i in 0..<N)
{
    var err = abs(reconstructed[i] - signal[i]);
    if (err > maxReconErr) maxReconErr = err;
}

// Parseval's theorem: time energy = freq energy
var timeEnergy = 0.0;
foreach (var i in 0..<N)
    timeEnergy += signal[i] ** 2;

var freqEnergy = 0.0;
foreach (var k in 0..<N)
    freqEnergy += realPart[k] ** 2 + imagPart[k] ** 2;
freqEnergy /= N;

var parsevalErr = abs(timeEnergy - freqEnergy);

// Peak detection in one-sided spectrum
var peaks = new int[3];
var peakMags = new double[3];

foreach (var k in 1..<(N / 2))
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

var peaksCorrect = peaks[0] == 1 and peaks[1] == 3 and peaks[2] == 5;
var reconstructOk = maxReconErr < 1e-10;
var parsevalOk = parsevalErr < 1e-8;

var amp1 = Math.Round(peakMags[0], 1);
var amp3 = Math.Round(peakMags[1], 1);
var amp5 = Math.Round(peakMags[2], 1);

var result = $"peaksCorrect={peaksCorrect}|bins={peaks[0]},{peaks[1]},{peaks[2]}|";
result += $"amp1={amp1}|amp3={amp3}|amp5={amp5}|";
result += $"reconstructOk={reconstructOk}|parsevalOk={parsevalOk}";

return result;
