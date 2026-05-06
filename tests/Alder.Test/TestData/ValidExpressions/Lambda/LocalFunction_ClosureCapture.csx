// §13.6.4: local function — captures outer variable
int multiplier = 3;
int Multiply(int x) => x * multiplier;
return Multiply(7);
