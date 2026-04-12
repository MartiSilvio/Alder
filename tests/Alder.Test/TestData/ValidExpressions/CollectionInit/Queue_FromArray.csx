// §12.8.16.2: Queue<int> constructor from IEnumerable
var q = new Queue<int>(new[] { 1, 2, 3 });
return q.Dequeue();
