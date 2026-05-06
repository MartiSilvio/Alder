// §20.5: Action<T> instantiation with captured local variable
int count = 0;
Action<int> printer = x => count += x;
printer(5);
printer(10);
return count;
