var count = 0; var iterations = 0; var i = 0; while (i < 5) { iterations++; i++; continue; count++; } return iterations * 100 + count;
