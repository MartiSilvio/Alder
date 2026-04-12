// §13.8.3: switch statement — string comparison is case-sensitive
string s = "Hello";
switch (s)
{
    case "hello": return 1;
    case "Hello": return 2;
    case "HELLO": return 3;
    default: return 0;
}
