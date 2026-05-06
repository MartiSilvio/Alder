// §20.5: Func with three parameters, instantiated from a lambda
Func<int, int, int, int> sum3 = (a, b, c) => a + b + c;
return sum3(1, 2, 3);
