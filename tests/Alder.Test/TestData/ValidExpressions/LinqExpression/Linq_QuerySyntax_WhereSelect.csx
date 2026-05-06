var list = new List<int> { 1, 2, 3, 4, 5 };
var result = from x in list where x > 2 select x * 2;
return result.Count();
