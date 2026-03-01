{
    var list = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    var result = from x in list
                 where x % 2 == 0
                 let doubled = x * 2
                 where doubled > 10
                 orderby doubled descending
                 select doubled;
    var sum = 0;
    foreach (var item in result) sum += (int)item;
    return sum;
}
