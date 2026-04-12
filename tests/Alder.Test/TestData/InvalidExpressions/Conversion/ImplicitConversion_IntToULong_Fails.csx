// §10.2.3: no implicit conversion from int to ulong (int can be negative)
int x = 42;
ulong u = x;
return u;
