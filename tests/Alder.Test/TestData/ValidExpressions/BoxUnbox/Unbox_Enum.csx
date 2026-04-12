// §10.3.7: enums box and unbox like their underlying type
object o = DayOfWeek.Monday;
return (DayOfWeek)o == DayOfWeek.Monday;
