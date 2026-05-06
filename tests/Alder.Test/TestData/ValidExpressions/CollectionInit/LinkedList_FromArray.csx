// §12.8.16.2: LinkedList<int> constructor from IEnumerable
var ll = new LinkedList<int>(new[] { 1, 2, 3 });
return ll.First.Value;
