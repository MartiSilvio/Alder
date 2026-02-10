{ var x = 0; try { throw new Exception(); } catch (Exception) { x = 1; } finally { x = 2; } return x; }
