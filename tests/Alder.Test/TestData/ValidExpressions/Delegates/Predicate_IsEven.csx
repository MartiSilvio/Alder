// §20.5: Predicate<T> delegate instantiation via lambda
Predicate<int> isEven = n => n % 2 == 0;
return isEven(4);
