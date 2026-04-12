{
    var dict = new Dictionary<string, int>();
    dict.Add("a", 1);
    dict.TryGetValue("a", out var val);
    return val;
}
