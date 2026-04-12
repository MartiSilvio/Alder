// §13.8.2: dangling else associates with nearest if
int x = 1;
int y = 0;
if (x > 0)
    if (x > 10)
        y = 1;
    else
        y = 2;
return y;
