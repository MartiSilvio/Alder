// §12.21.2: one expression assigns to several different targets
int a = 0;
int b = 0;
int c = 0;
a = (b = (c = 7) + 1) + 1;
return a * 100 + b * 10 + c;
