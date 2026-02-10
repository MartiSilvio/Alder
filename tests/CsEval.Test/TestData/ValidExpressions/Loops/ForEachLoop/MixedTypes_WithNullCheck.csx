var types = ""; foreach (var item in new object[] { 1, "hello", true, null }) { if (item == null) { types = types + "null,"; } else { types = types + "val,"; } } return types;
