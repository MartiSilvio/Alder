// Mandelbrot set ASCII renderer
// Exercises: double arithmetic, complex number math, string indexing, nested iteration

var width = 60;
var height = 24;
var maxIter = 50;
var palette = " .:-=+*%@#";

// View window in complex plane
var xMin = -2.0;
var xMax = 0.8;
var yMin = -1.2;
var yMax = 1.2;

var output = "";
var totalInSet = 0;
var totalIterations = 0;
var histogram = new int[10]; // 10 iteration buckets

for (var py = 0; py < height; py++)
{
    var ci = yMin + (yMax - yMin) * py / height;
    var line = "";

    for (var px = 0; px < width; px++)
    {
        var cr = xMin + (xMax - xMin) * px / width;

        // Mandelbrot iteration: z = z^2 + c
        var zr = 0.0;
        var zi = 0.0;
        var iter = 0;

        while (iter < maxIter && zr * zr + zi * zi <= 4.0)
        {
            var newZr = zr * zr - zi * zi + cr;
            zi = 2.0 * zr * zi + ci;
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

            // Bucket the iteration count
            var bucket = iter * 10 / maxIter;
            if (bucket > 9) bucket = 9;
            histogram[bucket]++;
        }
    }

    output += line;
    if (py < height - 1) output += "|";
}

// Compute statistics
var avgIter = totalIterations / (width * height);
var perimeterInSet = 0;

for (var px = 0; px < width; px++)
{
    for (var py = 0; py < height; py++)
    {
        if (px != 0 && px != width - 1 && py != 0 && py != height - 1) continue;

        var cr2 = xMin + (xMax - xMin) * px / width;
        var ci2 = yMin + (yMax - yMin) * py / height;
        var zr2 = 0.0;
        var zi2 = 0.0;
        var iter2 = 0;
        while (iter2 < maxIter && zr2 * zr2 + zi2 * zi2 <= 4.0)
        {
            var newZr2 = zr2 * zr2 - zi2 * zi2 + cr2;
            zi2 = 2.0 * zr2 * zi2 + ci2;
            zr2 = newZr2;
            iter2++;
        }
        if (iter2 == maxIter) perimeterInSet++;
    }
}

// Histogram non-empty bucket count
var nonEmptyBuckets = 0;
for (var i = 0; i < 10; i++)
{
    if (histogram[i] > 0) nonEmptyBuckets++;
}

var result = $"size={width}x{height}|inSet={totalInSet}|avgIter={avgIter}|";
result += $"perimeterInSet={perimeterInSet}|buckets={nonEmptyBuckets}|";
result += $"totalPixels={width * height}|totalIter={totalIterations}";

return result;
