string s = null;
return s switch
{
    null => "was null",
    "" => "empty",
    _ => "other"
};
