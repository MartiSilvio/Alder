// §12.14.2: conditional logical AND — short-circuits when left is false
int x = 0;
bool result = false && (++x > 0);
return result == false && x == 0;
