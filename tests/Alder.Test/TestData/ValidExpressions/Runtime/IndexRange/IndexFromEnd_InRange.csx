var arr = new int[] { 10, 20, 30, 40, 50 };
var slice = arr[1..^1];
return ((int[])slice).Length;
