// §8.3.11: tuple with heterogeneous element types
(int, string, double) t = (7, "hello", 2.5);
return t.Item1 + t.Item2.Length + (int)t.Item3;
