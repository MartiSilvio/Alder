// §11.2.2: declaration pattern 'int y' is not compatible with operand of type string
string s = "hello";
if (s is int y)
    return y;
return 0;
