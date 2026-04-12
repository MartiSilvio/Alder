// §13.6.4: two local functions declared in the same scope
int Double(int x) => x * 2;
int Triple(int x) => x * 3;
return Double(5) + Triple(5);
