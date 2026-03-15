var width = 60;
var height = 24;
var maxIter = 50;
var palette = " .:-=+*%@#";

var xMin = -2.0;
var xMax = 0.8;
var yMin = -1.2;
var yMax = 1.2;

var output = "";
var totalInSet = 0;
var totalIterations = 0;
var histogram = new int[10];

foreach (var py in 0..<height)
{
    var ci = yMin + (yMax - yMin) * py / height;
    var line = "";

    foreach (var px in 0..<width)
    {
        var cr = xMin + (xMax - xMin) * px / width;

        var zr = 0.0;
        var zi = 0.0;
        var iter = 0;

        while (iter < maxIter && zr ** 2 + zi ** 2 <= 4.0)
        {
            var newZr = zr ** 2 - zi ** 2 + cr;
            zi = 2 * (zr * zi) + ci;
            zr = newZr;
            iter++;
        }

        totalIterations += iter;

        if (iter == maxIter)
        {
            totalInSet++;
            line += "#";
        }
        else
        {
            var idx = iter % 10;
            line += palette[idx].ToString();

            var bucket = iter * 10 / maxIter;
            if (bucket > 9) bucket = 9;
            histogram[bucket]++;
        }
    }

    output += line;
    if (py < height - 1) output += "|";
}

var avgIter = totalIterations / (width * height);
var perimeterInSet = 0;

foreach (var px in 0..<width)
{
    foreach (var py in 0..<height)
    {
        if (px != 0 && px != width - 1 && py != 0 && py != height - 1) continue;

        var cr2 = xMin + (xMax - xMin) * px / width;
        var ci2 = yMin + (yMax - yMin) * py / height;
        var zr2 = 0.0;
        var zi2 = 0.0;
        var iter2 = 0;
        while (iter2 < maxIter && zr2 ** 2 + zi2 ** 2 <= 4.0)
        {
            var newZr2 = zr2 ** 2 - zi2 ** 2 + cr2;
            zi2 = 2 * (zr2 * zi2) + ci2;
            zr2 = newZr2;
            iter2++;
        }
        if (iter2 == maxIter) perimeterInSet++;
    }
}

var nonEmptyBuckets = 0;
foreach (var i in 0..<10)
{
    if (histogram[i] > 0) nonEmptyBuckets++;
}

var result = $"size={width}x{height}|inSet={totalInSet}|avgIter={avgIter}|";
result += $"perimeterInSet={perimeterInSet}|buckets={nonEmptyBuckets}|";
result += $"totalPixels={width * height}|totalIter={totalIterations}";

return result;
