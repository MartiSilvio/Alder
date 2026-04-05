var count = 0;
Func<bool> f = () => { count++; return false; };
var r = true || f();
return count;
