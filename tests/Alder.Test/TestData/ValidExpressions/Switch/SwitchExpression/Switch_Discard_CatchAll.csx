object o = 3.14;
return o switch
{
    int i => "int",
    string s => "string",
    _ => "other"
};
