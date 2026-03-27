var items = new[] { 1, 2, 3, 4, 5 };
var s = items.Sum();
var a = (int)items.Average();
var c = items.Count();
var mn = items.Min();
var mx = items.Max();
return s + a + c + mn + mx;
