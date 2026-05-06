// §12.16: throw expression in null coalescing — throws when null
string s = null;
try
{
    string result = s ?? throw new ArgumentNullException("s");
    return false;
}
catch (ArgumentNullException ex)
{
    return ex.ParamName == "s";
}
