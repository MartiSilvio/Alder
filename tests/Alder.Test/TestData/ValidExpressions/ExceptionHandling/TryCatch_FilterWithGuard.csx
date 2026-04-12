int code = 42;
try
{
    throw new Exception("boom");
}
catch (Exception) when (code == 42)
{
    return "matched";
}
catch (Exception)
{
    return "fallback";
}
