var point = (X: 3, Y: 0);
return point switch
{
    (0, 0) => "origin",
    (var x, 0) when x > 0 => "pos x-axis",
    (var x, 0) when x < 0 => "neg x-axis",
    (0, var y) when y > 0 => "pos y-axis",
    _ => "other"
};
