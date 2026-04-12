// §13.6.4: local function may be called before its declaration in source order
int result = Compute(5);
int Compute(int x) => x * x + 1;
return result;
