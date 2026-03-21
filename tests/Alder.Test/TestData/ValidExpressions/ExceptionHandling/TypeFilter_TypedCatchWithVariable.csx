{ var r = ""; try { throw new ArgumentException("msg"); } catch (ArgumentException ex) { r = ex.Message; } return r; }
