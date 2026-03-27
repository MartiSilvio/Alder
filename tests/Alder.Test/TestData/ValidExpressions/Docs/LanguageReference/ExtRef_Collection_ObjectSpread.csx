var obj = new { A = 1, B = 2 };
var merged = new { ..obj, C = 3 };
return (int)merged["A"] + (int)merged["B"] + (int)merged["C"];
