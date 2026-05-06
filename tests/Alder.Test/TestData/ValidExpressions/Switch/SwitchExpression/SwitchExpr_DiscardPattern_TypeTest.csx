object o = 42;
return o switch
{
    int => "int",
    _ => "other"
};
