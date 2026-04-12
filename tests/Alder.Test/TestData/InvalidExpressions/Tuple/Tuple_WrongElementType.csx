// Second element cannot be implicitly converted from int to string: CS0029
(int, string) t = (1, 2);
return t.Item1;
