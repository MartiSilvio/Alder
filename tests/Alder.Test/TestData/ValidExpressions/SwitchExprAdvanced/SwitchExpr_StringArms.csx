// §11.2: switch expression on string
string s = "beta";
return s switch
{
    "alpha" => 1,
    "beta" => 2,
    "gamma" => 3,
    _ => 0
};
