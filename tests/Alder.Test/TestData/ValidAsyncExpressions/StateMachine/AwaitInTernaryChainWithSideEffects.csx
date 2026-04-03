var log = new List<string>();
for (var i = 0; i < 4; i++)
{
    var val = await Task.FromResult(i);
    var tag = "";
    if (val == 0) tag = "zero";
    else if (val == 1) tag = "one";
    else tag = i % 2 == 0 ? await Task.FromResult("even") : await Task.FromResult("odd");
    log.Add(tag);
}
return string.Join(",", log);
