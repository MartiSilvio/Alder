// §18: Interface dispatch via IEnumerable<T>
IEnumerable<int> e = new int[] { 1, 2, 3 };
return e.Count();
