var result = "";
try
{
    int.Parse("not a number");
}
catch (FormatException ex)
{
    result = "caught";
}
return result;
