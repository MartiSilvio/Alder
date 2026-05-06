int x = 42;
return x switch
{
    int n when n > 100 => "big",
    int n when n > 10 => "medium",
    _ => "small"
};
