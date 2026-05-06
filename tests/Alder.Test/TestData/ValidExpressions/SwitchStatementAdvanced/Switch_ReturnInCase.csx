// §13.8.3: return inside case body
int n = 2;
switch (n)
{
    case 1:
        return "one";
    case 2:
        return "two";
    default:
        return "other";
}
