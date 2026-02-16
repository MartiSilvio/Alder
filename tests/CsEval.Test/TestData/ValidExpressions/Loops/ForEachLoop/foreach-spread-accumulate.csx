{
    var results = [];
    foreach (var i in [1, 2, 3]) {
        var x = i * 10;
        results = [..results, x];
    }
    return results;
}
