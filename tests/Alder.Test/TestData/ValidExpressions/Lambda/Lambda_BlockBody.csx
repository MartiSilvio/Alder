// §12.19.3: anonymous function — block body with return
Func<int, int> f = x =>
{
    int doubled = x * 2;
    return doubled + 1;
};
return f(10);
