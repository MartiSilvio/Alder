{ var x = 0; try { try { throw new Exception(); } catch (Exception) { x = 1; } finally { x = x + 10; } } catch (Exception) { x = -1; } return x; }
