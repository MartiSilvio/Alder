var jagged = new int[3][];
jagged[0] = new int[] { 1, 2 };
jagged[1] = new int[] { 3, 4, 5 };
jagged[2] = new int[] { 6 };
var total = 0;
for (var i = 0; i < jagged.Length; i++)
    for (var j = 0; j < jagged[i].Length; j++)
        total += jagged[i][j];
total + ":" + jagged[0].Length + "," + jagged[1].Length + "," + jagged[2].Length
