// §12.4.7.3: binary numeric promotion — byte + byte promotes to int
byte a = 100;
byte b = 200;
var result = a + b;
return result.GetType() == typeof(int) && result == 300;
