var count = 0;
for (var i = 0; i < 10; i++)
{
    for (var j = 0; j < 10; j++)
    {
        var val = await Task.FromResult(i * j);
        if (val > 20) break;
        count++;
    }
    var check = await Task.FromResult(i);
    if (check >= 5) break;
}
return count;
