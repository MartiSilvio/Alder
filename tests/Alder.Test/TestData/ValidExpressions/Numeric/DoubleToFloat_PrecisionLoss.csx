// §10.3.2: double to float narrowing loses precision for high-precision values
double d = 0.1 + 0.2;
float f = (float)d;
return (double)f != d;
