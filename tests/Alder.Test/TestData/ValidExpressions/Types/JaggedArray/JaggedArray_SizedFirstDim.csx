// §12.8.16.5: new int[3][] allocates only the outer dimension, inner rows assigned separately
int[][] arr = new int[3][];
arr[0] = new int[] { 100 };
arr[1] = new int[] { 200, 201 };
arr[2] = new int[] { 300 };
return arr[0][0] + arr[1][1] + arr[2][0];
