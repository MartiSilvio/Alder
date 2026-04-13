// Known limitation: named arguments on local function calls are not supported.
// §12.6.2.1: local function invoked with named arguments
int Compute(int a, int b, int c) => a * 100 + b * 10 + c;
return Compute(c: 3, a: 1, b: 2);
