// §15.15: await value used as switch-expression arm input
var x = await Task.FromResult(2);
return x switch
{
    1 => "one",
    2 => "two",
    _ => "other"
};
