var input = "AAABBBCCDDDDAAEFFFFF";

var encoded = "";
var i = 0;
while (i < input.Length)
{
    var ch = input[i];
    var count = 1;
    while (i + count < input.Length && input[i + count] == ch)
    {
        count++;
    }
    encoded += count.ToString() + ch;
    i += count;
}

var decoded = "";
var j = 0;
while (j < encoded.Length)
{
    var numStr = "";
    while (j < encoded.Length && encoded[j].ToString() =~ "^[0-9]$")
    {
        numStr += encoded[j];
        j++;
    }

    if (j < encoded.Length)
    {
        var letter = encoded[j];
        j++;

        var repeat = 0;
        foreach (var k in 0..<numStr.Length)
        {
            repeat = repeat * 10 + (numStr[k] - '0');
        }

        decoded += letter.ToString() * repeat;
    }
}

var originalLen = input.Length;
var encodedLen = encoded.Length;
var isCompressed = encodedLen < originalLen;

var roundtripOk = decoded == input;

var result = $"input={input}|encoded={encoded}|decoded={decoded}|";
result += $"roundtrip={roundtripOk}|compressed={isCompressed}|";
result += $"originalLen={originalLen}|encodedLen={encodedLen}";

return result;
