// §13.8.3: type pattern case
object o = "hello";
int result = 0;
switch (o)
{
    case int i:
        result = i;
        break;
    case string s:
        result = s.Length;
        break;
    default:
        result = -1;
        break;
}
return result;
