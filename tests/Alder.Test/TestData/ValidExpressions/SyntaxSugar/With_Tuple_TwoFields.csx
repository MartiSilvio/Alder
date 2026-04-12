var t = (X: 1, Y: 2, Z: 3);
var t2 = t with { X = 10, Z = 30 };
return t2.X + t2.Y + t2.Z;
