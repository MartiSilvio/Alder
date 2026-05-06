// §13.8.3: case label must be a constant expression
int y = 5;
int x = 5;
switch (x)
{
    case y: return "matched";
    default: return "no match";
}
