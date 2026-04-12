// §11.2: switch expression with null arm then type pattern
object o = 42;
return o switch
{
    null => "null",
    int i => i + 100,
    _ => -1
};
