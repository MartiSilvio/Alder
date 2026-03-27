{
    var day = "Tuesday";
    switch (day)
    {
        case "Monday":
        case "Tuesday":
        case "Wednesday":
        case "Thursday":
        case "Friday":
            return "weekday";
        case "Saturday":
        case "Sunday":
            return "weekend";
        default:
            return "unknown";
    }
}
