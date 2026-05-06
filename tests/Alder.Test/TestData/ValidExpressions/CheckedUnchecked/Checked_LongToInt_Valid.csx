// §12.8.19: checked long to int with value in range
long x = 1000L;
return checked((int)x) == 1000;
