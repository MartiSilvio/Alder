var a = new List<int> { 1, 2, 3, 4 };
var b = new List<int> { 3, 4, 5, 6 };
var u = a.Union(b).Count();
var i = a.Intersect(b).Count();
var e = a.Except(b).Count();
return u + i + e;
