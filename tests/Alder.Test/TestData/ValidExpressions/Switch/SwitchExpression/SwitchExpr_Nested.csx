int x = 2;
int y = 3;
return x switch
{
    1 => "one",
    2 => y switch
    {
        3 => "two-three",
        _ => "two-other"
    },
    _ => "other"
};
