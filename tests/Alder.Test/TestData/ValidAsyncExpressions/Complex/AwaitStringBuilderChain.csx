// §15.15: StringBuilder fed by awaited values
var sb = new System.Text.StringBuilder();
sb.Append(await Task.FromResult("a"));
sb.Append(await Task.FromResult("b"));
sb.Append(await Task.FromResult("c"));
return sb.ToString();
