var set = new HashSet<int> { 1, 2, 3 };
var added = set.Add(4);
var dup = set.Add(2);
set.UnionWith(new[] { 5, 6 });
return set.Count + (added ? 1 : 0) + (dup ? 0 : 1);
