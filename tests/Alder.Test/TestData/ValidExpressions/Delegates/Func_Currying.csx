// §20.5: Delegate returning delegate (currying)
Func<int, Func<int, int>> adder = x => y => x + y;
Func<int, int> add5 = adder(5);
return add5(10);
