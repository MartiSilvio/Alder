// §6.4.5.3: UL suffix produces ulong
ulong v = 1UL;
return v.GetType() == typeof(ulong);
