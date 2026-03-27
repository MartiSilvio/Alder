var regular = "hello\tworld";
var verbatim = @"C:\Users";
var raw = """raw "content" here""";
var interp = $"2+2={2+2}";
var verbInterp = $@"path\{2+2}";
var rawInterp = $"""raw {2+2} interp""";
return regular.Length + verbatim.Length + raw.Length + interp.Length + verbInterp.Length + rawInterp.Length;
