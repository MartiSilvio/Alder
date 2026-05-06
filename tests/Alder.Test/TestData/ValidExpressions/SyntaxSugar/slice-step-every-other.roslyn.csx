{ var arr = new[] {1, 2, 3, 4, 5, 6}; return arr.Where((x, i) => i % 2 == 0).ToArray(); }
