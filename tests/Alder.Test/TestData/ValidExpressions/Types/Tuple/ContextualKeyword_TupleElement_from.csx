// §6.4.4: 'from' as tuple element name
var t = (from: 1, select: 2, where: 3);
return t.from + t.select + t.where;
