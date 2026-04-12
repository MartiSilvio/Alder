// §11.2: switch expression directly as return value
int n = 3;
return n switch
{
    1 => "a",
    2 => "b",
    3 => "c",
    _ => "z"
};
