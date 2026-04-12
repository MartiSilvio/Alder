double original = Math.PI;
long bits = BitConverter.DoubleToInt64Bits(original);
double restored = BitConverter.Int64BitsToDouble(bits);
return restored == original;
