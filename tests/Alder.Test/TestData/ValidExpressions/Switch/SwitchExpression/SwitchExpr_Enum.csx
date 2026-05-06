DayOfWeek day = DayOfWeek.Wednesday;
return day switch
{
    DayOfWeek.Saturday => "weekend",
    DayOfWeek.Sunday => "weekend",
    _ => "weekday"
};
