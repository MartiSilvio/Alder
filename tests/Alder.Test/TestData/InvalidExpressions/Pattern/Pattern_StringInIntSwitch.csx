// §11.2.3: string constant pattern is not convertible to int
int x = 5;
return x switch
{
    "one" => 1,
    _ => 0
};
