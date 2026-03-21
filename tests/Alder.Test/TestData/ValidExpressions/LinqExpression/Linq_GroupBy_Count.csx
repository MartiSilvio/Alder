var items = new[] { "a", "b", "a", "c", "b", "a" };
return items.GroupBy(x => x).Count();
