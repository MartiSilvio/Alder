int x = 5;
return x switch
{
    < 0 => "neg",
    0 => "zero",
    > 0 => "pos"
};
