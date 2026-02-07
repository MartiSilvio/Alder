{
    // Numerical integration using Simpson's rule and trapezoidal rule with lambda functions
    // Exercises: lambda-as-function, double arithmetic, Math.Sqrt, Math.Abs, function composition

    // Define functions as lambdas
    var square = (double x) => x * x;
    var cube = (double x) => x * x * x;

    // Simpson's rule
    var simpsonIntegrate = (Func<double, double> f, double a, double b, int n) => {
        var h = (b - a) / n;
        var sum = f(a) + f(b);

        for (var i = 1; i < n; i++)
        {
            var xi = a + i * h;
            var coeff = i % 2 == 0 ? 2.0 : 4.0;
            sum += coeff * f(xi);
        }

        return sum * h / 3.0;
    };

    // Trapezoidal rule for comparison
    var trapezoidIntegrate = (Func<double, double> f, double a, double b, int n) => {
        var h = (b - a) / n;
        var sum = (f(a) + f(b)) / 2.0;

        for (var i = 1; i < n; i++)
        {
            sum += f(a + i * h);
        }

        return sum * h;
    };

    // Test 1: integral of x^2 from 0 to 1 = 1/3
    var exact1 = 1.0 / 3.0;
    var simp1 = simpsonIntegrate(square, 0.0, 1.0, 100);
    var trap1 = trapezoidIntegrate(square, 0.0, 1.0, 100);
    var simpErr1 = Math.Abs(simp1 - exact1);
    var trapErr1 = Math.Abs(trap1 - exact1);

    // Test 2: integral of x^3 from 0 to 2 = 4
    var exact2 = 4.0;
    var simp2 = simpsonIntegrate(cube, 0.0, 2.0, 100);
    var trap2 = trapezoidIntegrate(cube, 0.0, 2.0, 100);
    var simpErr2 = Math.Abs(simp2 - exact2);
    var trapErr2 = Math.Abs(trap2 - exact2);

    // Test 3: integral of sqrt(x) from 0 to 4 = 16/3
    Func<double, double> sqrtFunc = (double x) => Math.Sqrt(x);
    var exact3 = 16.0 / 3.0;
    var simp3 = simpsonIntegrate(sqrtFunc, 0.0, 4.0, 200);
    var trap3 = trapezoidIntegrate(sqrtFunc, 0.0, 4.0, 200);
    var simpErr3 = Math.Abs(simp3 - exact3);
    var trapErr3 = Math.Abs(trap3 - exact3);

    // Test 4: integral of 1/(1+x^2) from 0 to 1 = pi/4
    Func<double, double> arctanDeriv = (double x) => 1.0 / (1.0 + x * x);
    var exact4 = 3.14159265358979 / 4.0;
    var simp4 = simpsonIntegrate(arctanDeriv, 0.0, 1.0, 200);
    var simpErr4 = Math.Abs(simp4 - exact4);

    // Pi approximation
    var piApprox = simp4 * 4.0;
    var piErr = Math.Abs(piApprox - 3.14159265358979);

    // Simpson should be more accurate than trapezoidal for polynomials
    var simpBetter1 = simpErr1 < trapErr1;
    var simpBetter2 = simpErr2 < trapErr2;
    var simpBetter3 = simpErr3 < trapErr3;

    // Convergence test
    var simp1_50 = simpsonIntegrate(square, 0.0, 1.0, 50);
    var err50 = Math.Abs(simp1_50 - exact1);
    var err100 = Math.Abs(simp1 - exact1);
    var converges = err100 <= err50;

    var result = $"simpBetter1={simpBetter1}|simpBetter2={simpBetter2}|simpBetter3={simpBetter3}|";
    result += $"piApprox={(int)(piApprox * 10000) / 10000.0}|piErr<0.001={piErr < 0.001}|";
    result += $"converges={converges}|";
    result += $"simpErr1<1e-10={simpErr1 < 0.0000000001}|trapErr1<1e-4={trapErr1 < 0.0001}|";
    result += $"simpErr2<1e-10={simpErr2 < 0.0000000001}|";
    result += $"simp3Close={simpErr3 < 0.001}";

    return result;
}
