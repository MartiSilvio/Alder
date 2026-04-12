// §13.11: cannot return from finally block (CS0157)
try
{
    return 1;
}
finally
{
    return 2;
}
