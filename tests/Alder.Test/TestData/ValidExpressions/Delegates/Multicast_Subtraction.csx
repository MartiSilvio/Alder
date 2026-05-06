// §20.5: Delegate removal with -=
int count = 0;
Action inc1 = () => count += 1;
Action inc2 = () => count += 10;
Action combined = inc1 + inc2;
combined -= inc1;
combined();
return count;
