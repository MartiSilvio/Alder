var square = (double x) => x ** 2;
var cube = (double x) => x ** 3;

var simpsonIntegrate = (Func<double, double> f, double a, double b, int n) => {
    var h = (b - a) / n;
    var sum = f(a) + f(b);

    foreach (var i in 1..<n)
    {
        var xi = a + i * h;
        var coeff = i % 2 == 0 ? 2.0 : 4.0;
        sum += coeff * f(xi);
    }

    return sum * h / 3.0;
};

var trapezoidIntegrate = (Func<double, double> f, double a, double b, int n) => {
    var h = (b - a) / n;
    var sum = (f(a) + f(b)) / 2.0;

    foreach (var i in 1..<n)
    {
        sum += f(a + i * h);
    }

    return sum * h;
};

var exact1 = 1.0 / 3.0;
var simp1 = simpsonIntegrate(square, 0.0, 1.0, 100);
var trap1 = trapezoidIntegrate(square, 0.0, 1.0, 100);
var simpErr1 = abs(simp1 - exact1);
var trapErr1 = abs(trap1 - exact1);

var exact2 = 4.0;
var simp2 = simpsonIntegrate(cube, 0.0, 2.0, 100);
var trap2 = trapezoidIntegrate(cube, 0.0, 2.0, 100);
var simpErr2 = abs(simp2 - exact2);
var trapErr2 = abs(trap2 - exact2);

Func<double, double> sqrtFunc = (double x) => sqrt(x);
var exact3 = 16.0 / 3.0;
var simp3 = simpsonIntegrate(sqrtFunc, 0.0, 4.0, 200);
var trap3 = trapezoidIntegrate(sqrtFunc, 0.0, 4.0, 200);
var simpErr3 = abs(simp3 - exact3);
var trapErr3 = abs(trap3 - exact3);

Func<double, double> arctanDeriv = (double x) => 1.0 / (1.0 + x ** 2);
var exact4 = pi / 4;
var simp4 = simpsonIntegrate(arctanDeriv, 0.0, 1.0, 200);
var simpErr4 = abs(simp4 - exact4);

var piApprox = 4simp4;
var piErr = abs(piApprox - pi);

var simpBetter1 = simpErr1 < trapErr1;
var simpBetter2 = simpErr2 < trapErr2;
var simpBetter3 = simpErr3 < trapErr3;

var simp1_50 = simpsonIntegrate(square, 0.0, 1.0, 50);
var err50 = abs(simp1_50 - exact1);
var err100 = abs(simp1 - exact1);
var converges = err100 <= err50;

var result = $"simpBetter1={simpBetter1}|simpBetter2={simpBetter2}|simpBetter3={simpBetter3}|";
result += $"piApprox={(int)(piApprox * 10000) / 10000.0}|piErr<0.001={piErr < 0.001}|";
result += $"converges={converges}|";
result += $"simpErr1<1e-10={simpErr1 < 0.0000000001}|trapErr1<1e-4={trapErr1 < 0.0001}|";
result += $"simpErr2<1e-10={simpErr2 < 0.0000000001}|";
result += $"simp3Close={simpErr3 < 0.001}";

return result;
