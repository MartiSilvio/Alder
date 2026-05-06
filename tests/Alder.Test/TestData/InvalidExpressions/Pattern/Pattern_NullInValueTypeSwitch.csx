// §11.2.3: null pattern cannot match a non-nullable value type (int)
int x = 5;
return x switch
{
    null => 0,
    _ => 1
};
