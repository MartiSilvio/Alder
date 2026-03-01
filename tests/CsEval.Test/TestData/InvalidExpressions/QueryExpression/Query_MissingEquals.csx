{
    var a = new[] { 1, 2 };
    var b = new[] { 2, 3 };
    return (from x in a join y in b on x == y select x);
}
