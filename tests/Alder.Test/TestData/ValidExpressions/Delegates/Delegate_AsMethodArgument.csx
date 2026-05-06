// §20.5: Delegate instance passed as method argument
Func<int, bool> isPositive = x => x > 0;
int[] arr = new[] { -1, 2, -3, 4, -5, 6 };
return arr.Count(isPositive);
