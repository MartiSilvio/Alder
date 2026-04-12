// Cannot apply + between nullable int and string (not a valid lifted op)
int? x = 5;
return x + true;
