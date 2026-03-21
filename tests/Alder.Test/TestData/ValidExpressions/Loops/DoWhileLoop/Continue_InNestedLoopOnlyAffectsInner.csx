var total = 0; var i = 0; do { var j = 0; do { j = j + 1; if (j == 3) { continue; } total = total + 1; } while (j < 5); i = i + 1; } while (i < 3); return total;
