// §12.21.2: simple assignment — chained, returns assigned value
int a = 0, b = 0, c = 0;
a = b = c = 42;
return a == 42 && b == 42 && c == 42;
