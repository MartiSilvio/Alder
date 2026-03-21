var jagged = new int[][] {
    new int[] { 1, 2, 3 },
    new int[] { 4, 5, 6 }
};
jagged[0][1] = 99;
return jagged[0][1];
