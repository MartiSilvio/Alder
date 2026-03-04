var results = Array.Empty<int>();
for (var i = 0; i < 3; i = i + 1) {
    var x = i * 10;
    results = results.Append(x).ToArray();
}
return results;
