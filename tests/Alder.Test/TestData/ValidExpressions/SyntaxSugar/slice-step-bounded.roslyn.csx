{ var arr = new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}; var result = new List<int>(); for (var i = 1; i < 8; i += 2) result.Add(arr[i]); return result.ToArray(); }
