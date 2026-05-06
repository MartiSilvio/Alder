{ var r = ""; try { throw new InvalidOperationException(); } catch (ArgumentException) { r = "arg"; } catch { r = "bare"; } return r; }
