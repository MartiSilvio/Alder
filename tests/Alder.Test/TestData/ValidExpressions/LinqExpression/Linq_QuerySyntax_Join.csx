var left = new List<int> { 1, 2, 3, 4 };
var right = new List<int> { 2, 4, 6 };
var result = from l in left join r in right on l equals r select l;
return result.Count();
