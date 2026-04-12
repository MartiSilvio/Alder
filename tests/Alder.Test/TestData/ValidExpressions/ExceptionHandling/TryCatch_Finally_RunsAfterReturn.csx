var box = new int[1];
int Inner()
{
    try
    {
        return 1;
    }
    finally
    {
        box[0] = 42;
    }
}
int r = Inner();
return r + box[0];
