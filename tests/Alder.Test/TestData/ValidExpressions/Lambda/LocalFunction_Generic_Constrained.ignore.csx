// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
T Max<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0 ? a : b;
return Max(10, 25);
