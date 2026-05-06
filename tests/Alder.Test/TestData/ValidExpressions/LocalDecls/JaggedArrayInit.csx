// §17.2: jagged array initialized from implicitly-typed elements
int[][] jagged = new[] { new[] { 1, 2 }, new[] { 3, 4 } };
return jagged[0][0] + jagged[0][1] + jagged[1][0] + jagged[1][1];
