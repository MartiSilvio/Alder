var terms = 15;

// Factorial computed iteratively
var factorial = (int n) => {
    var f = 1.0;
    foreach (var i in 2..n) f *= i;
    return f;
};

// Taylor series for sin(x): sum of (-1)^k * x^(2k+1) / (2k+1)!
var taylorSin = (double x) => {
    var result = 0.0;
    foreach (var k in 0..<terms)
    {
        var exp = 2 * k + 1;
        var sign = k % 2 == 0 ? 1.0 : -1.0;
        result += sign * (x ** exp) / factorial(exp);
    }
    return result;
};

// Taylor series for cos(x): sum of (-1)^k * x^(2k) / (2k)!
var taylorCos = (double x) => {
    var result = 0.0;
    foreach (var k in 0..<terms)
    {
        var exp = 2 * k;
        var sign = k % 2 == 0 ? 1.0 : -1.0;
        result += sign * (x ** exp) / factorial(exp);
    }
    return result;
};

// Taylor series for e^x: sum of x^k / k!
var taylorExp = (double x) => {
    var result = 0.0;
    foreach (var k in 0..<terms)
    {
        result += (x ** k) / factorial(k);
    }
    return result;
};

var testX = pi / 6.0;

var sinApprox = taylorSin(testX);
var sinExact = sin(testX);
var sinErr = abs(sinApprox - sinExact);

var cosApprox = taylorCos(testX);
var cosExact = cos(testX);
var cosErr = abs(cosApprox - cosExact);

var expApprox = taylorExp(1.0);
var expExact = Math.E;
var expErr = abs(expApprox - expExact);

// sin^2 + cos^2 should be ~1
var identity = sinApprox * sinApprox + cosApprox * cosApprox;
var identityErr = abs(identity - 1.0);

var result = $"sinApprox={Math.Round(sinApprox, 10)}|sinExact={Math.Round(sinExact, 10)}|sinErr<1e-10={sinErr < 1e-10}|";
result += $"cosApprox={Math.Round(cosApprox, 10)}|cosExact={Math.Round(cosExact, 10)}|cosErr<1e-10={cosErr < 1e-10}|";
result += $"expApprox={Math.Round(expApprox, 10)}|expErr<1e-10={expErr < 1e-10}|";
result += $"identity={Math.Round(identity, 10)}|identityErr<1e-10={identityErr < 1e-10}";

return result;
