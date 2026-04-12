// §11.2: switch expression with when clauses selecting by range
int n = 25;
return n switch
{
    int x when x < 0 => "neg",
    int x when x < 10 => "small",
    int x when x < 50 => "medium",
    _ => "big"
};
