// §11.2.3: "if the pattern input value's type is an integral type or an enum type, the pattern's constant value converted to that type ... matches if e == v is true"
DayOfWeek d = DayOfWeek.Monday;
return d is DayOfWeek.Monday;
