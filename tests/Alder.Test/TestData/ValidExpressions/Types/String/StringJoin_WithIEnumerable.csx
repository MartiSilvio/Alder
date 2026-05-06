var words = "hello world foo bar".Split(' ');
var result = string.Join("-", words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)));
return result;
