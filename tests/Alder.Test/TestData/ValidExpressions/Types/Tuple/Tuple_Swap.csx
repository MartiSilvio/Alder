// §12.21.2: tuple assignment used as swap idiom
int a = 1;
int b = 2;
(a, b) = (b, a);
return a * 10 + b;
