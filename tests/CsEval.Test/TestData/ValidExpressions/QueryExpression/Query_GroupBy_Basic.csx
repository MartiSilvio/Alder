{
    var list = new[] { 1, 2, 3, 4, 5, 6 };
    var result = from x in list
                 group x by x % 2 == 0;
    var count = 0;
    foreach (var g in result) count += g.Count();
    return count;
}
