IEnumerable<int> Fibs() {
    var a = 0;
    var b = 1;
    while (true) {
        yield return a;
        var c = a + b;
        a = b;
        b = c;
    }
}
Fibs().Take(8).ToList()