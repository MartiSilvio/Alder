var source = new[] { 1, 2, 3, 4, 5 };
var result = (from x in source
              where x > 2
              orderby x descending
              select x * 10).ToList();
return result.Count == 3 && result[0] == 50 && result[2] == 30;
