// CS0160: a previous catch clause already catches all exceptions of this or a super type
try
{
    return 1;
}
catch (Exception)
{
    return 2;
}
catch (Exception)
{
    return 3;
}
