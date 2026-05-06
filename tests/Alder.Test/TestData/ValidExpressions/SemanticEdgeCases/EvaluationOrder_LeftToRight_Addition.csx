var log = "";
Func<int, int> f = (x) => { log += x.ToString(); return x; };
var r = f(1) + f(2) + f(3);
return log;
