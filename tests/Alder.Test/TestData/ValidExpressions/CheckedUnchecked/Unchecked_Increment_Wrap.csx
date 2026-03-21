int x = int.MaxValue;
x = unchecked(x + 1);
int y = int.MinValue;
y = unchecked(y - 1);
x == int.MinValue && y == int.MaxValue
