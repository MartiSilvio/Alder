var ll = new LinkedList<int>();
ll.AddLast(2);
ll.AddLast(3);
ll.AddFirst(1);
ll.AddLast(4);
return ll.First.Value + ll.Last.Value + ll.Count;
