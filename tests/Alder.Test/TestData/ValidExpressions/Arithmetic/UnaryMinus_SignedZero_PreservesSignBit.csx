double doubleZero = 0.0;
float floatZero = 0.0f;

double negatedDoubleZero = -doubleZero;
float negatedFloatZero = -floatZero;

return BitConverter.DoubleToInt64Bits(negatedDoubleZero) == long.MinValue &&
       BitConverter.SingleToInt32Bits(negatedFloatZero) == int.MinValue;
