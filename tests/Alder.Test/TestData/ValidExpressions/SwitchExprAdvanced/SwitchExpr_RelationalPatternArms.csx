// §11.2: switch expression with relational pattern arms
int n = 50;
return n switch
{
    < 0 => "neg",
    0 => "zero",
    < 10 => "low",
    < 100 => "mid",
    _ => "high"
};
