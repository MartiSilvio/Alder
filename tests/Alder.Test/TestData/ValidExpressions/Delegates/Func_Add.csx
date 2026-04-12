// §20.5: Func<T1,T2,TResult> two-parameter instantiation via lambda
Func<int, int, int> add = (a, b) => a + b;
return add(3, 4);
