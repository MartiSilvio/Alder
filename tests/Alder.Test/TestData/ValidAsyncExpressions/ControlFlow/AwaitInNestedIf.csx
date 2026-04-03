var a = await Task.FromResult(true);
var b = await Task.FromResult(false);
if (a)
{
    if (b) { return 1; }
    else { return await Task.FromResult(2); }
}
return 3;
