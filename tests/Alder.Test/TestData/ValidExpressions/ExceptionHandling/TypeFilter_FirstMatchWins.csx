{ var r = ""; try { throw new ArgumentException(); } catch (ArgumentNullException) { r = "derived"; } catch (ArgumentException) { r = "base"; } return r; }
