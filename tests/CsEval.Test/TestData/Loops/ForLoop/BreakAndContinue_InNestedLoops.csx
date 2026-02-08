var total = 0; for (var i = 1; i <= 5; i++) { if (i == 3) { continue; } for (var j = 1; j <= 5; j++) { if (j == 2) { break; } total++; } } return total;
