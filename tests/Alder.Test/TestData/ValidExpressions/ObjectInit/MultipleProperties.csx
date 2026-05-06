// §12.8.16.3: multiple properties set in one initializer
var ex = new Exception { Source = "src", HelpLink = "http://example.com" };
return ex.Source + "|" + ex.HelpLink;
