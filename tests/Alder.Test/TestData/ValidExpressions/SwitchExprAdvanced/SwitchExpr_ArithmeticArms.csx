// §11.2: switch expression with arithmetic in arms
int n = 4;
return n switch
{
    1 => n * 10,
    2 => n * 20,
    3 => n * 30,
    _ => n * 100
};
