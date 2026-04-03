{
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    return a + b;
}
