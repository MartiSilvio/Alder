// §11.3: subsequent pattern is subsumed by a prior pattern
int x = 5;
return x switch
{
    int n => n,
    1 => 100,
    _ => 0
};
