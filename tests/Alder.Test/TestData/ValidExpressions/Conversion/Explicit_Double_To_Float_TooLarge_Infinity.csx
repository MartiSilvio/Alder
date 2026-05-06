// §10.3.2: "If the magnitude of the double value is too large to represent as a float, the result becomes infinity with the same sign as the value"
double d = double.MaxValue;
float f = (float)d;
return float.IsPositiveInfinity(f);
