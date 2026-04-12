// §19: Bitwise-xor on a [Flags] enum toggles bits.
var a = System.IO.FileAttributes.ReadOnly | System.IO.FileAttributes.Hidden;
var b = a ^ System.IO.FileAttributes.Hidden;
return b == System.IO.FileAttributes.ReadOnly;
