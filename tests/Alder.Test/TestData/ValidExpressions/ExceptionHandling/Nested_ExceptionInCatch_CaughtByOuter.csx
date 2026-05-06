{ var r = ""; try { try { throw new Exception("orig"); } catch (Exception) { throw new InvalidOperationException("new"); } } catch (InvalidOperationException ex) { r = ex.Message; } return r; }
