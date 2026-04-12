// §6.4.5.4: m suffix produces decimal
decimal v = 1.0m;
return v.GetType() == typeof(decimal);
