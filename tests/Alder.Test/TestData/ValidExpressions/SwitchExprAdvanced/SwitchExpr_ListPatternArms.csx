// §11.2: switch expression with list pattern arms
int[] arr = { 1, 2, 3 };
return arr switch
{
    [] => "empty",
    [_] => "one",
    [_, _] => "two",
    [_, _, _] => "three",
    _ => "many"
};
