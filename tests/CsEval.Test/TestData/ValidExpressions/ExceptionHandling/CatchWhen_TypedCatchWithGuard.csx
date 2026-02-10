{ var r = 0; try { throw new ArgumentException("test"); } catch (ArgumentException ex) when (ex.Message == "test") { r = 1; } return r; }
