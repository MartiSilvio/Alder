// §10.3.2: "For a conversion from decimal to an integral type ... if the resulting integral value is outside the range of the destination type, a System.OverflowException is thrown"
decimal big = decimal.MaxValue;
return (long)big;
