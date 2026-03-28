var f1 = (Func<int, int>)(x => x * 2);
var f2 = (Func<int, int, int>)((a, b) => a + b);
var f3 = (Func<int>)(() => 42);
return f1(5) == 10 && f2(3, 4) == 7 && f3() == 42;
