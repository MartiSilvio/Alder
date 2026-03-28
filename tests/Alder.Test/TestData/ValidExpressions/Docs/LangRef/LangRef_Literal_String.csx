var a = "hello\nworld";
var b = @"C:\Users\path";
var c = """raw content""";
var d = $"Hello {"world"}";
return a.Contains("\n") && b.Contains("\\") && c == "raw content" && d == "Hello world";
