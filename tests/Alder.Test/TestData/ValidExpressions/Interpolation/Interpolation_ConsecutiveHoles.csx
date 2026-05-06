// §12.8.3: interpolated string — multiple consecutive holes
int a = 1;
int b = 2;
return $"{a}{b}{a + b}";
