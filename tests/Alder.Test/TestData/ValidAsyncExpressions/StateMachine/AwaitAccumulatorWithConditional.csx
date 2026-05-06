var evens = 0;
var odds = 0;
for (var i = 1; i <= 10; i++)
{
    var val = await Task.FromResult(i);
    if (val % 2 == 0)
        evens += await Task.FromResult(val);
    else
        odds += await Task.FromResult(val);
}
return evens - odds;
