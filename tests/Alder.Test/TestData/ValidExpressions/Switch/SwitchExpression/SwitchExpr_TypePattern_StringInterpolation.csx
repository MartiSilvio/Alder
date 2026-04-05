object obj = "hello";
return obj switch
{
    int i => $"int:{i}",
    string s => $"str:{s}",
    _ => "unknown"
};
