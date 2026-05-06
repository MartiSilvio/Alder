{ var r = ""; try { try { throw new ArgumentException("inner"); } catch (ArgumentException) { throw; } } catch (Exception ex) { r = ex.Message; } return r; }
