// §12.8.19: checked byte cast throws when source exceeds byte range
int big = 300;
return checked((byte)big);
