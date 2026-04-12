// §13.6.4 + §9.2.6: local function with ref parameter mutates caller's variable
void Increment(ref int x) => x++;
int n = 10;
Increment(ref n);
Increment(ref n);
return n;
