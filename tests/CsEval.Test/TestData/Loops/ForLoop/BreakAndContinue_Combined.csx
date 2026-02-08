var sum = 0; for (var i = 1; i <= 20; i++) { if (i % 2 == 0) { continue; } if (i > 10) { break; } sum += i; } return sum;
