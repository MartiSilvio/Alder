{
    var a = new[] { 1, 2 };
    var b = new[] { 10, 20 };
    var result = from x in a
                 from y in b
                 let sum = x + y
                 orderby sum descending
                 select sum;
    var total = 0;
    foreach (var item in result) total += (int)item;
    return total;
}
