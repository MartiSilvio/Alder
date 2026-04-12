// §10.3.2: "If the double value is NaN, the result is also NaN"
double d = double.NaN;
float f = (float)d;
return float.IsNaN(f);
