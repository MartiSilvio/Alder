{
    var list = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    return (from x in list where x % 2 == 0 && x > 5 select x * 10).ToList();
}