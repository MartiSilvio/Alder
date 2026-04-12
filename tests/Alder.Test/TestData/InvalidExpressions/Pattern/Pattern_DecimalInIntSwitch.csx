// §11.2.3: constant pattern type must be convertible to the switch governing type (int)
int x = 5;
return x switch
{
    1 => "one",
    2.5m => "half",
    _ => "other"
};
