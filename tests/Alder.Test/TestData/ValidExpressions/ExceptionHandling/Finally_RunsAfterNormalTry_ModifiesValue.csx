{ var x = 0; try { x = 1; } catch (Exception) { x = -1; } finally { x = x + 10; } return x; }
