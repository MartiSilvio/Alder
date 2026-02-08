{ var x = 0; for (var i = 0; i < 5; i++) { try { if (i == 2) break; x += i; } catch (Exception) { } } return x; }
