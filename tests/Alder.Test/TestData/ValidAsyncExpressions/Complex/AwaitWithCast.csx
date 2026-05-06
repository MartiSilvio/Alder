var obj = await Task.FromResult((object)42);
var num = (int)obj;
return num * 2;
