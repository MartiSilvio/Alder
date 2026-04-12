// §15.15: await within an if condition
if (await Task.FromResult(true))
    return "yes";
return "no";
