var threw = false;
var x = int.MaxValue;
try { checked { var y = x + 1; } }
catch (System.OverflowException) { threw = true; }
return threw;
