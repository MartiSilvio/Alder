{ var x = 0; try { throw new Exception(); x = 1; } catch (Exception) { x = 2; } return x; }
