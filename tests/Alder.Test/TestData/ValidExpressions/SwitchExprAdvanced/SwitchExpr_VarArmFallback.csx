// §11.2: switch expression with var arm as fallback
int n = 42;
return n switch
{
    1 => "one",
    2 => "two",
    var x => $"other:{x}"
};
