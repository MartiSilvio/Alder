var point = (1, 0);
return point switch
{
    (0, 0) => "origin",
    (1, 0) => "right",
    (0, 1) => "up",
    _ => "other"
};
