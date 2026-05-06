// §13.6.4: two local functions that call each other (mutual recursion)
bool IsEven(int n) => n == 0 ? true : IsOdd(n - 1);
bool IsOdd(int n) => n == 0 ? false : IsEven(n - 1);
return IsEven(10) && IsOdd(7);
