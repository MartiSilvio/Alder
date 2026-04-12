var point = (0, 0);
return point switch
{
    (0, 0) => "origin",
    (var x, 0) => "x-axis",
    (0, var y) => "y-axis",
    _ => "other"
};
