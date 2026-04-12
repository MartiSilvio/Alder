// §11.2: switch expression tuple — x-axis arm
var p = (5, 0);
return p switch
{
    (0, 0) => "origin",
    (_, 0) => "x-axis",
    (0, _) => "y-axis",
    _ => "other"
};
