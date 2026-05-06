var a = new List<int> { 1, 2, 3 };
var b = new List<int> { 3, 2, 1 };
var reversed = a.AsEnumerable().Reverse().ToList();
return reversed.SequenceEqual(b) && a.Contains(2);
