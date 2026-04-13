// Known limitation: a `when` guard cannot read an enclosing local — the guard binding scope does
// not inherit from the surrounding block, so `code == 42` evaluates false and the fallback runs.
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
