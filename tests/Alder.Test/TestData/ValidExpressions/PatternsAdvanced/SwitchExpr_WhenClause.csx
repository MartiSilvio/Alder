// §11.2: switch expression with when clause
int x = 15;
return x switch
{
    int n when n > 10 => "big",
    int n when n > 0 => "small",
    _ => "other"
};
