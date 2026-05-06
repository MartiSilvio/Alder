var skipped = 0; var processed = 0; for (var i = 0; i < 10; i++) { if (i < 5) { skipped++; continue; } processed++; } return skipped * 100 + processed;
