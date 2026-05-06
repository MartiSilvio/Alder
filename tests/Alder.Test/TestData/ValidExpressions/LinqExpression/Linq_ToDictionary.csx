var list = new List<int> { 1, 2, 3 };
var dict = list.ToDictionary(x => x, x => x * x);
return dict[3];
