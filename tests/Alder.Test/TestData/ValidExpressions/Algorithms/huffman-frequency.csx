// Huffman coding frequency analysis and code assignment
// Exercises: dictionaries, sorting, string manipulation, complex data processing
var text = "ABRACADABRA";

// Count character frequencies using parallel arrays
var chars = new[] { 'A', 'B', 'C', 'D', 'R', '\0', '\0', '\0' };
var freqs = new[] { 0, 0, 0, 0, 0, 0, 0, 0 };
var numChars = 5;

for (var i = 0; i < text.Length; i++)
{
    var ch = text[i];
    for (var j = 0; j < numChars; j++)
    {
        if (chars[j] == ch)
        {
            freqs[j]++;
            break;
        }
    }
}

// Sort by frequency (bubble sort on parallel arrays)
for (var i = 0; i < numChars - 1; i++)
{
    for (var j = 0; j < numChars - i - 1; j++)
    {
        if (freqs[j] > freqs[j + 1])
        {
            var tf = freqs[j]; freqs[j] = freqs[j + 1]; freqs[j + 1] = tf;
            var tc = chars[j]; chars[j] = chars[j + 1]; chars[j + 1] = tc;
        }
    }
}

// Build frequency report (sorted ascending)
var freqReport = "";
for (var i = 0; i < numChars; i++)
{
    if (i > 0) freqReport += ",";
    freqReport += $"{chars[i]}:{freqs[i]}";
}

// Assign simple prefix codes based on frequency order
// Lower frequency = longer code (simplified Huffman-like)
var codes = new[] { "0000", "0001", "001", "01", "1" };
var codeReport = "";
for (var i = 0; i < numChars; i++)
{
    if (i > 0) codeReport += ",";
    codeReport += $"{chars[i]}={codes[i]}";
}

// Encode the original text
var encoded = "";
for (var i = 0; i < text.Length; i++)
{
    var ch = text[i];
    for (var j = 0; j < numChars; j++)
    {
        if (chars[j] == ch)
        {
            encoded += codes[j];
            break;
        }
    }
}

// Calculate compression stats
var originalBits = text.Length * 8;
var encodedBits = encoded.Length;
var ratio = encodedBits * 100 / originalBits;

var result = $"text={text}|freqs={freqReport}|codes={codeReport}|";
result += $"encoded={encoded}|";
result += $"originalBits={originalBits}|encodedBits={encodedBits}|ratio={ratio}%";

return result;
