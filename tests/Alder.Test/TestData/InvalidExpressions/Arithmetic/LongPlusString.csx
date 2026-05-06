// §12.10.5: long + string yields string; cannot implicitly convert back to long
long a = 10L;
string b = "5";
long c = a + b;
return c;
