var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
return list.Skip(3).Take(4).Sum();
