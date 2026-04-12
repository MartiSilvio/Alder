object obj = "hi";
if (obj is string { Length: 2 } s)
{
    return s;
}
return "no";
