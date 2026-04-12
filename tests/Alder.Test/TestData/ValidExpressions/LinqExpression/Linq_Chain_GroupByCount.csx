// §12.8.9.3: chained extension methods — GroupBy→Count
var words = new List<string> { "apple", "banana", "avocado", "blueberry", "cherry" };
return words.GroupBy(w => w[0]).Count();
