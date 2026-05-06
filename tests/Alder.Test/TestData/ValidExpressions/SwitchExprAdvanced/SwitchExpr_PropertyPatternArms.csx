// §11.2: switch expression with property pattern arms
string s = "hello";
return s switch
{
    { Length: 0 } => "empty",
    { Length: 1 } => "single",
    { Length: < 5 } => "short",
    _ => "long"
};
