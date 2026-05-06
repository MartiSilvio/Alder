// §12.19.2: anonymous function — explicitly typed parameters
Func<int, string, string> f = (int x, string s) => $"{s}={x}";
return f(42, "answer");
