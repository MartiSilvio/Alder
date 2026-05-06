// §12.14.2: conditional logical OR — short-circuits when left is true
int x = 0;
bool result = true || (++x > 0);
return result == true && x == 0;
