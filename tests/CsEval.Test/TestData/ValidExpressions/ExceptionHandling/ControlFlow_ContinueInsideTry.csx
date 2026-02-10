{ var x = 0; for (var i = 0; i < 3; i++) { try { if (i == 1) continue; x += i; } catch (Exception) { } } return x; }
