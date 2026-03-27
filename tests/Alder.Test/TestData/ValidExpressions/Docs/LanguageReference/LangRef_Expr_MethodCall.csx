var upper = "hello".ToUpper();
var joined = string.Join(", ", new[] { "a", "b" });
var parsed = int.TryParse("42", out var n);
return upper + "|" + joined + "|" + parsed + "|" + n;
