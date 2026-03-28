var obj = new { Name = "Alice", Age = 30 };
var result = new { ..obj, Age = 31 };
return result["Name"];
