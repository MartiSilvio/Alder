var dict = new Dictionary<string, object> { ["x"] = 5 };
var counter = 0;
dict["x"] ??= ++counter;
return counter;