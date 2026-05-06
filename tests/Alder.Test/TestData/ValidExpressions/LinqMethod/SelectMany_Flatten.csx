var groups = new[] { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5 } };
return groups.SelectMany(g => g).Sum();
