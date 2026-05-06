// §13.9.5: foreach element type string cannot convert to int iteration variable
foreach (int x in new string[] { "a" })
    return x;
return 0;
