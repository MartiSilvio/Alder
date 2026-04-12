object obj = "hello";
return obj switch
{
    int i => "int",
    string s => "string",
    double d => "double",
    _ => "other"
};
