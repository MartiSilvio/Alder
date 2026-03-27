var a = 3.CompareTo(5) < 0 ? -1 : 3.CompareTo(5) > 0 ? 1 : 0;
var b = 5.CompareTo(5) < 0 ? -1 : 5.CompareTo(5) > 0 ? 1 : 0;
var c = 7.CompareTo(5) < 0 ? -1 : 7.CompareTo(5) > 0 ? 1 : 0;
return a + b + c;
