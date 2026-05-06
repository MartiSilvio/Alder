// §12.16: Throw as expression on right side of null-coalesce
string s = "value";
try
{
    string v = s ?? throw new InvalidOperationException("null");
    return v;
}
catch (InvalidOperationException)
{
    return "err";
}
