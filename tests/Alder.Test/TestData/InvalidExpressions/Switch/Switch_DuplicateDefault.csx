// Cannot have two default labels in a switch statement
int x = 1;
switch (x)
{
    default: return "a";
    default: return "b";
}
