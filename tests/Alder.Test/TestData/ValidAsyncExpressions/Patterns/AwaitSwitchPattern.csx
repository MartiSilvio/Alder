var x = await Task.FromResult((object)"hello");
return x switch
{
    int n => n.ToString(),
    string s => s.ToUpper(),
    _ => "unknown"
};
