var input = await Task.FromResult(42);
var category = "";
if (input < 0) category = "negative";
else if (input == 0) category = "zero";
else if (input < 10) category = "small";
else if (input < 100) category = "medium";
else category = "large";

var detail = category == "medium" ? await Task.FromResult("just right") : await Task.FromResult("other");
return detail;
