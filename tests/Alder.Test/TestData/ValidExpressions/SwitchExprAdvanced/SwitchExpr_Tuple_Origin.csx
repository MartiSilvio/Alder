// §11.2: switch expression on tuple positional patterns
var p = (0, 0);
return p switch
{
    (0, 0) => "origin",
    (_, 0) => "x-axis",
    (0, _) => "y-axis",
    _ => "other"
};
