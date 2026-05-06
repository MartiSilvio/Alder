var target = 7;
for (var i = 0; i < 20; i++) {
    if (i == target) {
        return $"Found at {i}";
    }
}
return "Not found";
