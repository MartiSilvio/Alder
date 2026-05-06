// §12.19: anonymous function — multiple parameters
Func<int, int, int> add = (a, b) => a + b;
return add(3, 4);
