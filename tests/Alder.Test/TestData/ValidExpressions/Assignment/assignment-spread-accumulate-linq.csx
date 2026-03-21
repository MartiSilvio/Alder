var items = [1, 2, 3];
items = [..items, 4, 5];
var filtered = items.Where(x => x > 2).ToList();
return filtered;
