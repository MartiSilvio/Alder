// §13.6.4: static local function — cannot capture enclosing state
static int Square(int x) => x * x;
return Square(6);
