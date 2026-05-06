// §13.6.4: void local function with side effect on captured variable
int counter = 0;
void Bump() => counter++;
Bump();
Bump();
Bump();
return counter;
