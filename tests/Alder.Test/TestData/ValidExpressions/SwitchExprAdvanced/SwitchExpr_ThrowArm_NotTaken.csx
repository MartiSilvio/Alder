// §11.2: switch expression with throw expression arm (not taken)
int n = 1;
return n switch
{
    1 => "ok",
    _ => throw new InvalidOperationException("unreachable")
};
