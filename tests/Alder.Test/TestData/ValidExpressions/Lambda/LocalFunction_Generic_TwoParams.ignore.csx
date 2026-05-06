// Known limitation: generic type parameters on local functions are not supported.
T First<T, U>(T a, U b) => a;
return First(7, "ignored");
