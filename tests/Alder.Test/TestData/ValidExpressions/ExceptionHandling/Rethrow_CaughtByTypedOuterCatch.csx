{ var r = ""; try { try { throw new ArgumentException("test"); } catch (ArgumentException) { throw; } } catch (ArgumentException ex) { r = ex.Message; } return r; }
