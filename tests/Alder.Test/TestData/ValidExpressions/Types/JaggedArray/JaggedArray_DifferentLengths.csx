var jagged = new int[][] {
    new int[] { 1 },
    new int[] { 2, 3 },
    new int[] { 4, 5, 6 }
};
return jagged[0].Length + jagged[1].Length + jagged[2].Length;
