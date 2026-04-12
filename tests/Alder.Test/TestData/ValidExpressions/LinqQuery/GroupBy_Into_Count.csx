// §12.20: group-into continuation then count each group
var list = new[] { 1, 2, 3, 4, 5, 6 };
return (from x in list group x by x % 2 into g select g.Count()).Sum();
