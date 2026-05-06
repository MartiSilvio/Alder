// §12.8.9.3: chained extension methods — Where→Select→Sum
var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
return list.Where(x => x % 2 == 0).Select(x => x * x).Sum();
