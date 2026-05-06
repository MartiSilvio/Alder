// §12.14.2: conditional logical OR — evaluates second operand when left is false
int x = 0;
bool result = false || (++x > 0);
return result == true && x == 1;
