{ var r = ""; try { throw new ArgumentException(); } catch (InvalidOperationException) { r = "wrong"; } catch (ArgumentException) { r = "right"; } return r; }
