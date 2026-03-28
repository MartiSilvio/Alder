var a = typeof(int) == typeof(System.Int32);
var b = sizeof(int) == 4;
var c = nameof(System.Int32) == "Int32";
var d = default(int) == 0;
return a && b && c && d;
