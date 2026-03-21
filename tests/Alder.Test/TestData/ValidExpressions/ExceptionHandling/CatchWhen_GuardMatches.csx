{ var r = 0; try { throw new Exception("match"); } catch (Exception ex) when (ex.Message == "match") { r = 1; } catch (Exception) { r = 2; } return r; }
