var count = 0;
Func<bool> f = () => { count++; return true; };
var r = false && f();
return count;
