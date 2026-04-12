// §15.15: manually constructed Task<T> requires explicit Start
var t = new Task<int>(() => 42);
t.Start();
return await t;
