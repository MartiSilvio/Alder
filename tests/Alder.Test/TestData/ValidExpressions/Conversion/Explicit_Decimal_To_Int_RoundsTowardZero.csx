// §10.3.2: "For a conversion from decimal to an integral type, the source value is rounded towards zero to the nearest integral value"
decimal pos = 3.9m;
decimal neg = -3.9m;
return (int)pos == 3 && (int)neg == -3;
