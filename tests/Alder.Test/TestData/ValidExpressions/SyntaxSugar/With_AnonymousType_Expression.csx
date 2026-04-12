var p = new { Name = "A", Age = 30 };
var q = p with { Age = p.Age + 1 };
return q.Age;
