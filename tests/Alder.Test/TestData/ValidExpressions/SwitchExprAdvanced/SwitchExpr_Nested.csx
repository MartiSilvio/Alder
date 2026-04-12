// §11.2: nested switch expressions
int outer = 1;
int inner = 2;
return outer switch
{
    1 => inner switch
    {
        1 => "1-1",
        2 => "1-2",
        _ => "1-?"
    },
    _ => "other"
};
