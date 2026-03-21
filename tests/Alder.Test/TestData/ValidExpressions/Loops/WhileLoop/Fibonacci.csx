var a = 0; var b = 1; var count = 0; while (count < 10) { var temp = a + b; a = b; b = temp; count++; } return b;
