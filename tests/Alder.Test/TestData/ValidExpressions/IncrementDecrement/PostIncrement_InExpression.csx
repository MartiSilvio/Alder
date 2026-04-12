// §12.8.15: postfix increment in expression — old value used in addition
int x = 3;
int result = x++ + 10;
return result == 13 && x == 4;
