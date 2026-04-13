// Known limitation: default parameter values on local functions are not supported.
// §13.6.4 + §15.6.2.1: local function with default parameter value
int F(int x = 5) => x * 2;
return F() + F(10);
