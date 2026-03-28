var x = 42;
var result = x switch
{
    < 0 => "negative",
    0 => "zero",
    > 0 => "positive"
};
return result == "positive";
