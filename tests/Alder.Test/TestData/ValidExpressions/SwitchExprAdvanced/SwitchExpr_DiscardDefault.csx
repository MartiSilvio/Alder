// §11.2: switch expression with discard default
int n = 99;
return n switch
{
    0 => "zero",
    _ => "other"
};
