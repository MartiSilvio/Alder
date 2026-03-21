var flags = System.IO.FileAttributes.ReadOnly | System.IO.FileAttributes.Hidden;
return (flags & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;
