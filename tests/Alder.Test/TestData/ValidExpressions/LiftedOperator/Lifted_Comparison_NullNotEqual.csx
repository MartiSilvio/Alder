// §12.4.8: lifted relational — null compared to value is false
int? a = null;
return (a > 5) == false && (a < 5) == false && (a == 5) == false;
