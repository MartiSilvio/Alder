// §12.21.2: deconstruct a ValueTuple returned from a lambda
Func<(string, int)> make = () => ("hello", 5);
var (first, second) = make();
return first + ":" + second;
