int x = 3;
return x switch
{
    1 or 2 or 3 => "low",
    4 or 5 or 6 => "mid",
    _ => "other"
};
