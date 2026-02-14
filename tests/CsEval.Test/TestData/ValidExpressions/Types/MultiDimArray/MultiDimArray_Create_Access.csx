{
    // Multi-dimensional array creation, assignment, and access
    var arr = new int[3, 3];
    var sum = 0;
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            arr[i, j] = i * 3 + j + 1;
        }
    }
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            sum = sum + arr[i, j];
        }
    }
    return sum;
}
