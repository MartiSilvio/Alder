var skipped = 0; var processed = 0; var i = 0; while (i < 10) { i++; if (i <= 5) { skipped++; continue; } processed++; } return skipped * 100 + processed;
