// §10.2.4: "implicit enumeration conversion permits a constant_expression with the value zero to be converted to any enum_type"
DayOfWeek d = 0;
return d == DayOfWeek.Sunday;
