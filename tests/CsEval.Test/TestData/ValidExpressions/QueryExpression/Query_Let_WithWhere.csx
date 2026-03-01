{
    var list = new[] { 1, 2, 3, 4, 5 };
    var result = from x in list let doubled = x * 2 where doubled > 6 select doubled;
    var sum = 0;
    foreach (var item in result) sum += (int)item;
    return sum;
}
