Func<int, Func<int, int>> multiply = x => y => x * y;
return multiply(3)(4);
