// §6.4.4: 'when' as tuple element name
var t = (when: true, result: 5);
return t.when ? t.result : 0;
