{
    // Run-length encoding and decoding
    // Exercises: string indexing, char comparison, while loops, string interpolation, nested logic
    var input = "AAABBBCCDDDDAAEFFFFF";

    // Encode
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

    // Decode the encoded string back
    var decoded = "";
    var j = 0;
    while (j < encoded.Length)
    {
        // Parse the number
        var numStr = "";
        while (j < encoded.Length && encoded[j] >= '0' && encoded[j] <= '9')
        {
            numStr += encoded[j];
            j++;
        }

        // Parse the character
        if (j < encoded.Length)
        {
            var letter = encoded[j];
            j++;

            // Convert numStr to int manually
            var repeat = 0;
            for (var k = 0; k < numStr.Length; k++)
            {
                repeat = repeat * 10 + (numStr[k] - '0');
            }

            // Append repeated character
            for (var k = 0; k < repeat; k++)
            {
                decoded += letter;
            }
        }
    }

    // Compression ratio
    var originalLen = input.Length;
    var encodedLen = encoded.Length;
    var isCompressed = encodedLen < originalLen;

    // Verify roundtrip
    var roundtripOk = decoded == input;

    var result = $"input={input}|encoded={encoded}|decoded={decoded}|";
    result += $"roundtrip={roundtripOk}|compressed={isCompressed}|";
    result += $"originalLen={originalLen}|encodedLen={encodedLen}";

    return result;
}
