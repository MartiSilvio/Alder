// §13.11: Exception filter 'when' clause
try
{
    throw new InvalidOperationException("bad");
}
catch (InvalidOperationException e) when (e.Message == "bad")
{
    return "matched";
}
