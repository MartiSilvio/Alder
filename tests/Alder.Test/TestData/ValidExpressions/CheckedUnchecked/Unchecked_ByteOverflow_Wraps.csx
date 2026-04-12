// §12.8.19: unchecked byte addition wraps through int cast
byte b = 255;
return unchecked((byte)(b + 1)) == 0;
