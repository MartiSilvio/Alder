// §17.2: jagged array (array of arrays) declaration and element access
int[][] jagged = new int[][] {
    new int[] { 1, 2 },
    new int[] { 3, 4, 5 },
    new int[] { 6 }
};
int total = 0;
for (int i = 0; i < jagged.Length; i++)
    for (int j = 0; j < jagged[i].Length; j++)
        total += jagged[i][j];
return total;
