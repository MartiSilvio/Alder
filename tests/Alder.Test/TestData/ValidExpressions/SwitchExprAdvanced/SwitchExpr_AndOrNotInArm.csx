// §11.2: switch expression with and/or/not combined in a single arm
int n = 5;
return n switch
{
    (> 0 and < 10) or 100 => "single-digit-or-hundred",
    not (< 0) => "non-negative",
    _ => "other"
};
