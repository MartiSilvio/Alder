// Relational pattern is only valid for numeric types, not string
string s = "hello";
return s switch
{
    > "a" => 1,
    _ => 0
};
