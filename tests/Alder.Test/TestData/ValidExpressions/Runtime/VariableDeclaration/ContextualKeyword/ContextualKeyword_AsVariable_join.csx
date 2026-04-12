// §6.4.4: 'join' is a contextual keyword, valid as identifier outside query expression
var join = ", ";
return string.Join(join, new[] { "a", "b", "c" });
