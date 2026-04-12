// §19: Bitwise-or of [Flags] enum values combines bits.
var combined = System.IO.FileAttributes.ReadOnly | System.IO.FileAttributes.Hidden;
return (combined & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly;
