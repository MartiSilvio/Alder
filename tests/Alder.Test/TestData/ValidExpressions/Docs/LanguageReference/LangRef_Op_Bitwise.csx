var andOp = 0xFF & 0x0F;
var orOp = 0xF0 | 0x0F;
var xorOp = 0xFF ^ 0x0F;
var notOp = ~0;
var lshift = 1 << 4;
var rshift = 16 >> 2;
return andOp + orOp + xorOp + notOp + lshift + rshift;
