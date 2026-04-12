// §12.8.19: checked negation of int.MinValue cannot be represented
int v = int.MinValue;
return checked(-v);
