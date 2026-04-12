// §13.6.4 + §9.2.7: local function with out parameter
void Divide(int a, int b, out int q, out int r)
{
    q = a / b;
    r = a % b;
}
Divide(17, 5, out int quotient, out int remainder);
return quotient * 10 + remainder;
