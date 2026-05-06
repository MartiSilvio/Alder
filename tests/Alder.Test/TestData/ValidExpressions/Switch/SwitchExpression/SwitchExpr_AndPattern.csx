int x = 5;
return x switch
{
    > 0 and < 10 => "small",
    >= 10 and < 100 => "medium",
    _ => "other"
};
