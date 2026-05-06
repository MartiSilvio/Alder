// §12.8.16.3: object initializer on Exception
var ex = new Exception { Source = "test" };
return ex.Source;
