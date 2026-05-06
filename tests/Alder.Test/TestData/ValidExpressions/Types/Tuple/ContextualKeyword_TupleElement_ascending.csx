// §6.4.4: query contextual keywords as tuple element names
var t = (ascending: 1, descending: 2, orderby: 3, group: 4, into: 5);
return t.ascending + t.descending + t.orderby + t.group + t.into;
