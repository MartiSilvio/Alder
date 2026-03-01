{
    var list = new[] { 10, 20, 30 };
    return (from x in list select x).ToList();
}