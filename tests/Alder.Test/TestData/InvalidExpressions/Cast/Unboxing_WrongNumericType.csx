// §10.3.7: "For an unboxing conversion to a given non_nullable_value_type to succeed at run-time, the value of the source operand shall be a reference to a boxed value of that non_nullable_value_type" — throws InvalidCastException otherwise
object o = 1.0;
return (int)o;
