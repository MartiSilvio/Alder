{ var r = 0; try { throw new Exception("other"); } catch (Exception ex) when (ex.Message == "match") { r = 1; } catch (Exception) { r = 2; } return r; }
