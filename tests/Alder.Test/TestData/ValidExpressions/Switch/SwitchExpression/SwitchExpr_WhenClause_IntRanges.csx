var x = 5;
return x switch
{
    int n when n > 10 => "big",
    int n when n > 0 => "small",
    _ => "other"
};
