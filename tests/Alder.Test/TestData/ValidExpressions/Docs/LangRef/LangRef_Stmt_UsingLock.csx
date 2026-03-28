var obj = new object();
var result = 0;
lock (obj) { result = 42; }
return result;
