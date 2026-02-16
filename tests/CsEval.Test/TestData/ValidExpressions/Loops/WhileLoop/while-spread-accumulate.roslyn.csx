{
    var i = 0;
    var results = Array.Empty<int>();
    while (i < 3) {
        var x = i * 10;
        results = results.Append(x).ToArray();
        i = i + 1;
    }
    return results;
}
