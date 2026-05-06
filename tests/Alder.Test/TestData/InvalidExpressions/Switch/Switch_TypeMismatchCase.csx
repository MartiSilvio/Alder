// §13.8.3: case label type must match switch expression governing type
int x = 5;
switch (x)
{
    case "hello": return "matched";
    default: return "no match";
}
