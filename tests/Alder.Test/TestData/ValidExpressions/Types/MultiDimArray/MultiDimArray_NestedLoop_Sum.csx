// §17.4, §17.7: multi-dimensional array initializer and rank-2 element access
int[,] arr = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
int total = 0;
for (int i = 0; i < arr.GetLength(0); i++)
    for (int j = 0; j < arr.GetLength(1); j++)
        total += arr[i, j];
return total;
