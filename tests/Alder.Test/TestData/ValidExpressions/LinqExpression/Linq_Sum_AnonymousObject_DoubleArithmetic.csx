var items = new[] { new { Count = 110 }, new { Count = 90 } };
var expected = 100.0;
return items.Sum(b => Math.Pow(b.Count - expected, 2) / expected);
