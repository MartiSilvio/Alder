{
    var main = new[] { 1, 2, 3, 4, 5 };
    var exclude = new[] { 2, 4 };
    return (from x in main
            where !(from e in exclude select e).Contains(x)
            select x).ToList();
}
