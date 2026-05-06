// §17.2: rectangular (multi-dimensional) array declaration
int[,] matrix = new int[2, 2];
matrix[0, 0] = 1;
matrix[0, 1] = 2;
matrix[1, 0] = 3;
matrix[1, 1] = 4;
return matrix[0, 0] + matrix[0, 1] + matrix[1, 0] + matrix[1, 1];
