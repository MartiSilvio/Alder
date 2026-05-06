var a = 0; var b = 1; var count = 0; do { var temp = a + b; a = b; b = temp; count = count + 1; } while (count < 10); return b;
