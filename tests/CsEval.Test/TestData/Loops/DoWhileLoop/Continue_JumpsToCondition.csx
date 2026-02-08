var skipped = 0; var processed = 0; var i = 0; do { i = i + 1; if (i <= 5) { skipped = skipped + 1; continue; } processed = processed + 1; } while (i < 10); return skipped * 100 + processed;
