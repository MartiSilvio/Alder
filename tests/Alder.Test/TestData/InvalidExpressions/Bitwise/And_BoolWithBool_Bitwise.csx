// §12.13: & is not defined for string/int operands
// However, testing & with string operands is invalid
string a = "hello";
int b = 5;
return a & b;
