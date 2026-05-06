var result = new[] { 1, 2, 3 }.SelectMany(x => new[] { x, x * 10 }).ToArray();
return result.Length;
