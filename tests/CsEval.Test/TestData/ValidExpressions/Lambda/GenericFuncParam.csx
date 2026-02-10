var square = (double x) => x * x;
var apply = (Func<double, double> f, double a) => f(a);
return apply(square, 5.0);
