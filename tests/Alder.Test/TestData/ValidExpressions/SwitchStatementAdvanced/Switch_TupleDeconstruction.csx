// §13.8.3: tuple positional pattern case
var p = (1, 2);
string result = "";
switch (p)
{
    case (0, 0):
        result = "origin";
        break;
    case (var x, var y) when x == y:
        result = "diagonal";
        break;
    case (1, 2):
        result = "one-two";
        break;
    default:
        result = "other";
        break;
}
return result;
