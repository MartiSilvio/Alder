// §11.2.2: declaration patterns require a pattern type. Nullable value types are invalid pattern types.
int x = 5;
if (x is int? y)
    return y;
return 0;
