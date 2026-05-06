// §13.6.4: local function — recursive factorial
int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
return Factorial(5);
