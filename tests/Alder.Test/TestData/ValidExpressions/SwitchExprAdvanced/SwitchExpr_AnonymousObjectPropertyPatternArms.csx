// §11.2: switch expression with property pattern arms on an anonymous object
var order = new { Total = 1200m, IsRush = true };
var t = order switch
{
    { Total: > 1000m, IsRush: true } => "premium-express",
    { Total: > 1000m } => "premium",
    { IsRush: true } => "express",
    _ => "standard"
};
return t;
