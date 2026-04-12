var items = new List<object> { 1, "two", 3, "four", 5 };
var ints = items.OfType<int>().Sum();
return ints;
