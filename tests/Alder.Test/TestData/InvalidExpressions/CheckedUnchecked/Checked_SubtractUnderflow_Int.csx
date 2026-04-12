// §12.8.19: checked subtraction underflow throws
int a = int.MinValue;
return checked(a - 1);
