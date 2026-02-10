{ var r = ""; try { try { throw new ArgumentException(); } catch (InvalidOperationException) { r = "inner"; } } catch (ArgumentException) { r = "outer"; } return r; }
