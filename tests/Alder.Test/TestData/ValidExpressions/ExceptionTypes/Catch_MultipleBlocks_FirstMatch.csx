// §13.11: Multiple catch blocks - first matching type wins
try
{
    throw new ArgumentNullException("p");
}
catch (ArgumentNullException)
{
    return 1;
}
catch (ArgumentException)
{
    return 2;
}
catch (Exception)
{
    return 3;
}
