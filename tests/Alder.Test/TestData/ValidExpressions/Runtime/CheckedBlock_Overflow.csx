var threw = false;
var x = int.MaxValue;
try { checked { var y = x + 1; } }
catch (OverflowException) { threw = true; }
return threw;
