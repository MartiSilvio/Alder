// §11.2: switch expression with null arm
string s = null;
return s switch
{
    null => "nothing",
    "" => "empty",
    _ => "has value"
};
