var total = 0; var i = 0; do { i = i + 1; if (i == 3) { continue; } var j = 0; do { j = j + 1; if (j == 2) { break; } total = total + 1; } while (j < 5); } while (i < 5); return total;
