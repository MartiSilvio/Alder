{ var r = ""; try { throw new Exception("test"); } catch (Exception ex) { r = ex.Message; } return r; }
