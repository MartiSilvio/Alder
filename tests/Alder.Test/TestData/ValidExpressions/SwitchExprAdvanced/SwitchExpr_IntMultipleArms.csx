// §11.2: switch expression on int with multiple arms
int n = 3;
return n switch
{
    1 => "one",
    2 => "two",
    3 => "three",
    4 => "four",
    _ => "other"
};
