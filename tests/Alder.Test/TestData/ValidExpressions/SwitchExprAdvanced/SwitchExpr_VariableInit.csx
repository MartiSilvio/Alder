// §11.2: switch expression used in variable initializer
int n = 2;
string label = n switch
{
    1 => "one",
    2 => "two",
    _ => "other"
};
return label;
