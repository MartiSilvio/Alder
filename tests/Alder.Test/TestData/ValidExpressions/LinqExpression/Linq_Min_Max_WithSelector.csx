var list = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
return list.Max(x => x * 2) - list.Min(x => x * 2);
