var inputs = new object[] { 1, "hello", 3.14, true, null };
var results = new List<string>();
foreach (var input in inputs)
{
    var val = await Task.FromResult(input);
    var desc = val switch
    {
        int n => $"int:{n}",
        string s => $"str:{s}",
        double d => $"dbl:{d}",
        bool b => $"bool:{b}",
        null => "null",
        _ => "unknown"
    };
    results.Add(desc);
}
return string.Join(",", results);
