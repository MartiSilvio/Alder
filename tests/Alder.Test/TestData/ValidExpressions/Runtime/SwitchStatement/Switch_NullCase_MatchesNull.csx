string? input = null;
var result = "";
switch (input) {
    case null:
        result = "is null";
        break;
    default:
        result = "not null";
        break;
}
return result;
