// §12.19.6: anonymous function — closure modifies outer variable
int counter = 0;
Action increment = () => counter++;
increment();
increment();
increment();
return counter;
