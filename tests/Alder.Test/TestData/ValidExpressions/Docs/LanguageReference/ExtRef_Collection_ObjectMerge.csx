var a = new { X = 1 };
var b = new { Y = 2 };
var merged = a + b;
return (int)merged["X"] + (int)merged["Y"];
