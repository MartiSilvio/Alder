{
    var list = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    return (from x in list where x > 3 where x < 8 select x).ToList();
}