int x = 15;
return x switch
{
    int n when n < 0 => "neg",
    int n when n > 10 => "big",
    _ => "small"
};
