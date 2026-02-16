{
    var results = Array.Empty<int>();
    foreach (var i in new[] { 1, 2, 3 }) {
        var x = i * 10;
        results = results.Append(x).ToArray();
    }
    return results;
}
