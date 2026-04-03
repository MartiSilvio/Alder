var sync = 10;
var async1 = await Task.FromResult(20);
var sync2 = sync + async1;
var async2 = await Task.FromResult(sync2 * 2);
return async2;
