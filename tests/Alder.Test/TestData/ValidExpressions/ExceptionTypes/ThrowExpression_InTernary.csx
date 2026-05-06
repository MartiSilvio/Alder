// §12.16: Throw as expression in conditional operator
int x = 5;
try
{
    int v = x < 0 ? throw new ArgumentException("neg") : x * 2;
    return v;
}
catch (ArgumentException)
{
    return -1;
}
