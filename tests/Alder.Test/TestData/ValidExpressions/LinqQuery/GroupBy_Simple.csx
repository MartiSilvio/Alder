// §12.20: group x by key forms IGrouping sequence
var list = new[] { 1, 2, 3, 4, 5, 6 };
return (from x in list group x by x % 2).Count();
