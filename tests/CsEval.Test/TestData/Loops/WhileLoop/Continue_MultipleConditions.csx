var sum = 0; var i = 0; while (i < 20) { i++; if (i % 2 == 0) { continue; } if (i % 3 == 0) { continue; } sum += i; } return sum;
