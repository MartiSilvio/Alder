object x = "hello";
return x switch
{
    not null => "has value",
    _ => "null"
};
