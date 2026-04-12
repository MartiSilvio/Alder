var list = new List<int> { 1, 2, 3 };
System.Collections.IEnumerable ie = list;
var e = ie.GetEnumerator();
return e.MoveNext();
