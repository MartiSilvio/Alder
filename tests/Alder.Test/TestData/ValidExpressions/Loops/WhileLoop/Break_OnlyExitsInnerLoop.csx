var outerCount = 0; var totalInner = 0; var i = 0; while (i < 3) { var j = 0; while (j < 10) { if (j == 2) { break; } totalInner++; j++; } outerCount++; i++; } return outerCount * 100 + totalInner;
