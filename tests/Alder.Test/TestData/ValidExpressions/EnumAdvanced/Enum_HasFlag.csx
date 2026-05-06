// §19: HasFlag returns true when all requested flag bits are set.
var v = System.IO.FileAttributes.ReadOnly | System.IO.FileAttributes.Hidden;
return v.HasFlag(System.IO.FileAttributes.ReadOnly);
