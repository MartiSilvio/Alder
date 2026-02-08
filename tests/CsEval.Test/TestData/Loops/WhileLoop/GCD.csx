var a = 48; var b = 18; while (b != 0) { var temp = b; b = a % b; a = temp; } return a;
