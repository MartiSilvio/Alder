{
    var list = new[] { 42 };
    return (from x in list select x * 2).ToList();
}
