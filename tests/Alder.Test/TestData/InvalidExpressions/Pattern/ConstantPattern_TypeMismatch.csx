// §11.2.3: constant pattern must be compatible with the input type
string s = "hello";
return s switch
{
    42 => "matched",
    _ => "no match"
};
