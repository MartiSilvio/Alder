var outerCount = 0; var totalInner = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 10; j++) { if (j == 2) { break; } totalInner++; } outerCount++; } return outerCount * 100 + totalInner;
