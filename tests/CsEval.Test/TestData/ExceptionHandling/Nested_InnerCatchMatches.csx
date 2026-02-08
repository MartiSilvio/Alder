{ var r = ""; try { try { throw new ArgumentException(); } catch (ArgumentException) { r = "inner"; } } catch (ArgumentException) { r = "outer"; } return r; }
