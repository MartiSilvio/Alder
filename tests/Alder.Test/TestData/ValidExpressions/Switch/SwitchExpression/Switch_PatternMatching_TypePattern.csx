object o = 42;
return o switch
{
    int i => $"int:{i}",
    string s => $"string:{s}",
    _ => "other"
};
