// §10.3.2: byte 200 reinterpreted as sbyte becomes -56 under unchecked
byte b = 200;
return unchecked((sbyte)b) == -56;
