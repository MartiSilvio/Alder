var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
return list.SkipWhile(x => x < 4).TakeWhile(x => x < 7).Sum();
