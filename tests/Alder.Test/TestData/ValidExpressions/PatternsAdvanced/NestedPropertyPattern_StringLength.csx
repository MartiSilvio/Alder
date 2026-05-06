// §11.2: nested property pattern via nested braces
var t = (Name: "hello", Age: 30);
return t is { Name: { Length: 5 } };
