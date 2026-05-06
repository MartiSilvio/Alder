// §11.2: switch expression returning nullable type
int n = 5;
int? result = n switch
{
    < 0 => null,
    0 => 0,
    _ => n * 2
};
return result;
