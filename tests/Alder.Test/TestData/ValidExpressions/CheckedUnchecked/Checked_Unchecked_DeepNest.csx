int a = int.MaxValue;
int safe = checked(unchecked(a + 1) + 0);
safe
