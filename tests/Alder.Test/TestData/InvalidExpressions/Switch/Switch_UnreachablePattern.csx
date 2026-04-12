// CS8510: pattern has already been handled by a previous arm
int x = 5;
return x switch
{
    > 0 => "pos",
    > 10 => "big",
    _ => "other"
};
