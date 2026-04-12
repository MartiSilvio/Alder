// §11.2: switch expression tuple — y-axis arm
var p = (0, 7);
return p switch
{
    (0, 0) => "origin",
    (_, 0) => "x-axis",
    (0, _) => "y-axis",
    _ => "other"
};
