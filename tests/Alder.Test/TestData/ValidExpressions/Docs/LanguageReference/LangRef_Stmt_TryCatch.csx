{
    try
    {
        var x = int.Parse("not a number");
        return x;
    }
    catch (FormatException)
    {
        return -1;
    }
}
