// §11.2: switch expression with type pattern capture arms
object o = "hello";
return o switch
{
    int i => i * 2,
    string s => s.Length,
    _ => -1
};
