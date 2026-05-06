var items = new[] { new { Score = 80 }, new { Score = 90 }, new { Score = 100 } };
return items.Average(x => x.Score);
