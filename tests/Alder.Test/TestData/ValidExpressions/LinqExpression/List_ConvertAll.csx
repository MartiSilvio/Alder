var list = new List<int> { 1, 2, 3, 4 };
var doubled = list.ConvertAll(x => x * 2);
return doubled.Sum();
