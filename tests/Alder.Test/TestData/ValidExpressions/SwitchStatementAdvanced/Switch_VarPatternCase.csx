// §13.8.3: var pattern case captures value
object o = 42;
string result = "";
switch (o)
{
    case var x when x is int i && i > 10:
        result = "big-int";
        break;
    case var x:
        result = x?.ToString() ?? "null";
        break;
}
return result;
