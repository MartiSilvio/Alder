Func<double, double> square = (double x) => x * x;
var apply = (Func<double, double> f, double a) => f(a);
return apply(square, 7.0);
