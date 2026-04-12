var q = new Queue<int>();
q.Enqueue(1);
q.Enqueue(2);
q.Enqueue(3);
var first = q.Dequeue();
var peeked = q.Peek();
return first + peeked + q.Count;
