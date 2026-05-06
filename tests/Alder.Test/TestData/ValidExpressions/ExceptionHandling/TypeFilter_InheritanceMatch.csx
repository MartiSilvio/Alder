{ var r = ""; try { throw new ArgumentNullException(); } catch (ArgumentException) { r = "base"; } return r; }
