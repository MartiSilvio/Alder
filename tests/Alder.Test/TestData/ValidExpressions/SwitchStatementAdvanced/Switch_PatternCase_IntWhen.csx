// §13.8.3: pattern case with when clause
object o = 42;
string result = "";
switch (o)
{
    case int n when n < 0:
        result = "neg";
        break;
    case int n when n > 0:
        result = "pos";
        break;
    case int n:
        result = "zero";
        break;
    default:
        result = "nonint";
        break;
}
return result;
