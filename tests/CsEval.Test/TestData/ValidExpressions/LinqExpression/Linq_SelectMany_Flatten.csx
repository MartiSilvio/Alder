var arrays = new[] { new[] { 1, 2 }, new[] { 3, 4 } };
return arrays.SelectMany(a => a).Sum();
