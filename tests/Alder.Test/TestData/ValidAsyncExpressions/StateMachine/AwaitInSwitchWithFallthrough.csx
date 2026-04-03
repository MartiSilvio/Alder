var results = new List<int>();
for (var i = 0; i < 4; i++)
{
    var x = await Task.FromResult(i);
    switch (x)
    {
        case 0:
            results.Add(await Task.FromResult(100));
            break;
        case 1:
            results.Add(await Task.FromResult(200));
            break;
        case 2:
            results.Add(await Task.FromResult(300));
            break;
        default:
            results.Add(await Task.FromResult(-1));
            break;
    }
}
return results.Sum();
