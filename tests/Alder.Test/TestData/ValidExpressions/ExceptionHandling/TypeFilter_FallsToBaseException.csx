{ var r = ""; try { throw new InvalidOperationException(); } catch (ArgumentException) { r = "arg"; } catch (Exception) { r = "fallback"; } return r; }
