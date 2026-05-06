// §12.8.9.2: method invocations — List.Add and List.Remove
var list = new List<int>();
list.Add(1);
list.Add(2);
list.Add(3);
list.Remove(2);
return list.Count;
