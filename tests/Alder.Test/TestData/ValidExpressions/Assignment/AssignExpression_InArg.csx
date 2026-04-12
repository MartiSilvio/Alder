// §12.21.2: assignment expression used directly as a method argument
int x = 0;
int Identity(int v) => v;
int result = Identity(x = 42);
return x + result;
