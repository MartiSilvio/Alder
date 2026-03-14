var jagged = new int[][] {
    new int[] { 1, 2 },
    new int[] { 3, 4 }
};
jagged[1] = new int[] { 10, 20 };
return jagged[1][0] + jagged[1][1];
