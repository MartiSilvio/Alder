var list = new List<int> { 5, 2, 8, 1, 4 };
var result = from x in list orderby x select x;
return result.First();
