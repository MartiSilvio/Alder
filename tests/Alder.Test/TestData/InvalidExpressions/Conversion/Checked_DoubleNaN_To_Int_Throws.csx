// §10.3.2: "In a checked context ... If the value of the operand is NaN or infinite, a System.OverflowException is thrown"
double nan = double.NaN;
return checked((int)nan);
