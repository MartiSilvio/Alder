// §20.5: Delegate instantiation via lambda conversion - Func<T,TResult>
Func<int, int> square = x => x * x;
return square(5);
