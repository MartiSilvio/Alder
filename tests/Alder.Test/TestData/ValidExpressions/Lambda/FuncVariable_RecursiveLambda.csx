Func<int, int> factorial = null;
factorial = n => n <= 1 ? 1 : n * factorial(n - 1);
return factorial(6);
