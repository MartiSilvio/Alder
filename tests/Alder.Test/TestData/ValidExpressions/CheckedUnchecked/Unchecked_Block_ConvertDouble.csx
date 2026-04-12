// §12.8.19: unchecked conversion from double.MaxValue to int does not throw
int result = unchecked((int)double.MaxValue);
return result == int.MinValue;
