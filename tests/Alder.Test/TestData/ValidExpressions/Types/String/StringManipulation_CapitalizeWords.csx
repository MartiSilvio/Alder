var words = "hello world foo bar".Split(' ');
var capitalized = words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)).ToArray();
var result = string.Join("-", capitalized);
return result;
