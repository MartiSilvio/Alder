var arr = new[] { 10, 20, 30, 40, 50 };
var last = arr[^1];
var slice = arr[1..3];
return last + slice.Length;
