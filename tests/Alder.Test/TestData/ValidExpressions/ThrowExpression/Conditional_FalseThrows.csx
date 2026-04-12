// §12.16: throw expression in conditional — false branch throws
try
{
    int x = false ? 42 : throw new InvalidOperationException("boom");
    return false;
}
catch (InvalidOperationException ex)
{
    return ex.Message == "boom";
}
