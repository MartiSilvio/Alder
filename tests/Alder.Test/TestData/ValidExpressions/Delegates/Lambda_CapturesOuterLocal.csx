// §12.19.6.2: Lambda closure over outer local
int outer = 100;
Func<int, int> addOuter = x => x + outer;
return addOuter(25);
