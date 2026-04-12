// §12.21.2: chained simple assignment is right-associative
int a = 0;
int b = 0;
int c = 0;
a = b = c = 5;
return a + b + c;
