// §11.2: switch expression falling through with discard arm
int n = 42;
return n switch
{
    1 => "one",
    2 => "two",
    _ => "other"
};
