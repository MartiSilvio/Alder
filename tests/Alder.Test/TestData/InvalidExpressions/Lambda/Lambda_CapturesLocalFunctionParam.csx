// Lambda with too many params for target delegate
Func<int, int> f = (x, y) => x + y;
return f(5);
