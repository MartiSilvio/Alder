{ var r = ""; try { try { throw new Exception("orig"); } catch (Exception ex) { throw ex; } } catch (Exception ex) { r = ex.Message; } return r; }
