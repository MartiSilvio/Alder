{
    var terms = 15;

    var factorial = (int n) => {
        var f = 1.0;
        for (var i = 2; i <= n; i++) f *= i;
        return f;
    };

    var taylorSin = (double x) => {
        var result = 0.0;
        for (var k = 0; k < terms; k++)
        {
            var exp = 2 * k + 1;
            var sign = k % 2 == 0 ? 1.0 : -1.0;
            result += sign * Math.Pow(x, exp) / factorial(exp);
        }
        return result;
    };

    var taylorCos = (double x) => {
        var result = 0.0;
        for (var k = 0; k < terms; k++)
        {
            var exp = 2 * k;
            var sign = k % 2 == 0 ? 1.0 : -1.0;
            result += sign * Math.Pow(x, exp) / factorial(exp);
        }
        return result;
    };

    var taylorExp = (double x) => {
        var result = 0.0;
        for (var k = 0; k < terms; k++)
        {
            result += Math.Pow(x, k) / factorial(k);
        }
        return result;
    };

    var testX = Math.PI / 6.0;

    var sinApprox = taylorSin(testX);
    var sinExact = Math.Sin(testX);
    var sinErr = Math.Abs(sinApprox - sinExact);

    var cosApprox = taylorCos(testX);
    var cosExact = Math.Cos(testX);
    var cosErr = Math.Abs(cosApprox - cosExact);

    var expApprox = taylorExp(1.0);
    var expExact = Math.E;
    var expErr = Math.Abs(expApprox - expExact);

    var identity = sinApprox * sinApprox + cosApprox * cosApprox;
    var identityErr = Math.Abs(identity - 1.0);

    var result = $"sinApprox={Math.Round(sinApprox, 10)}|sinExact={Math.Round(sinExact, 10)}|sinErr<1e-10={sinErr < 1e-10}|";
    result += $"cosApprox={Math.Round(cosApprox, 10)}|cosExact={Math.Round(cosExact, 10)}|cosErr<1e-10={cosErr < 1e-10}|";
    result += $"expApprox={Math.Round(expApprox, 10)}|expErr<1e-10={expErr < 1e-10}|";
    result += $"identity={Math.Round(identity, 10)}|identityErr<1e-10={identityErr < 1e-10}";

    return result;
}
