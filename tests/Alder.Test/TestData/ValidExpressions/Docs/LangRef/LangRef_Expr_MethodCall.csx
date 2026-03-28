var a = "hello".ToUpper();
var b = string.Join(", ", new[] { "a", "b" });
var c = new[] { 1, 2, 3 }.Where(x => x > 1).Count();
var d = Enumerable.Empty<int>().Count();
int.TryParse("42", out var n);
return a == "HELLO" && b == "a, b" && c == 2 && d == 0 && n == 42;
