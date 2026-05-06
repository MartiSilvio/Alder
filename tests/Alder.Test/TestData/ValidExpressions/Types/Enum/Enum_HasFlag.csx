System.IO.FileAttributes attrs = System.IO.FileAttributes.Hidden | System.IO.FileAttributes.ReadOnly;
return attrs.HasFlag(System.IO.FileAttributes.Hidden);
