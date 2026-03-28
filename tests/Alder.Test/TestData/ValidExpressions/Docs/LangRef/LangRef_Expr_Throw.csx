var x = (string?)null ?? "fallback";
var y = true ? "yes" : throw new Exception();
return x == "fallback" && y == "yes";
