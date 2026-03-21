var x = 255;
x <<= 0;
var noShift = x;
x = 1;
x <<= 63;
var maxShift = x;
return noShift;
