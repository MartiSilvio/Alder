object val = 42;
return val switch
{
    int n when n > 50 => "big",
    int n when n > 0 => "small",
    _ => "other"
};
