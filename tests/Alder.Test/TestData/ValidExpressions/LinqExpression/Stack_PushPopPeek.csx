var s = new Stack<int>();
s.Push(10);
s.Push(20);
s.Push(30);
var popped = s.Pop();
var peeked = s.Peek();
return popped + peeked + s.Count;
