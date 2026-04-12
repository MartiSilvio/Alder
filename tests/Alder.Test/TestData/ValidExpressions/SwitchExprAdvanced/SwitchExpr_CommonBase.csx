// §11.2: switch expression arms of different types upcast to object
int n = 1;
object result = n switch
{
    1 => "one",
    2 => 2,
    _ => null
};
return result.ToString();
