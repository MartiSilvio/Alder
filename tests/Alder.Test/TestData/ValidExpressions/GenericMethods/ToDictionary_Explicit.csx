var d = new[] { 1, 2, 3 }.ToDictionary<int, int>(k => k);
return d.Count;
