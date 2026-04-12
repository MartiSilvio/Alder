// §11.2: discard pattern in switch expression always matches
object o = 42;
return o switch
{
    _ => true
};
