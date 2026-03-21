{ var x = 0; try { throw new Exception(); } catch (Exception) { } finally { x = 42; } return x; }
