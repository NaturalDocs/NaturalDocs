
/* Topic: Char Literals
	_____________________________________________

	--- Code
	a 'x' a
	b '\'' b
	c '\"' c
	d '\\' d
	---
*/


/* Topic: Char Literal Prefixes
	_____________________________________________

	--- Code
	a u8'x' a
	b u'x' b
	c U'x' c
	d L'x' d
	---
*/


/* Topic: String Literals
	_____________________________________________

	--- Code
	a "x" a
	b "\'" b
	c "\"" c
	d "\\" d
	---
*/


/* Topic: String Literal Prefixes
	_____________________________________________

	--- Code
	a u8"x" a
	b u"x" b
	c U"x" c
	d L"x" d
	---
*/


/* Topic: Raw String Literals
	_____________________________________________

	There are no escaped characters inside a raw string.  They continue until the closing )".

	--- Code
	a R"(xxx'xxx"xxx\"xxx""xxx)" a
	---

	That means \)" DOES end the string, since the backslash doesn't escape it.

	--- Code
	a R"(xxx\)" a
	---

	Characters can appear between the " and ( which must be duplicated on the other side.  The sequence
	can be up to 16 characters long.

	--- Code
	a R"*(xxx"xxx)"xxx*xxx)*" a
	b R"--(xxx"xxx)"xxx--xxx)--" b
	c R"XYZ123(xxx"xxx)"xxxXYZ123xxx)XYZ123" c
	---

	They must be duplicated exactly on the other side.  If you use an opening symbol like [ or <, the
	same symbol must be on the other side, not the closing one.

	--- Code
	a R"[(xxx"xxx)"xxx)]"xxx)[" a
	b R"<<(xxx"xxx)"xxx)>>"xxx)<<" b
	---

*/


/* Topic: RawString Literal Prefixes
	_____________________________________________

	--- Code
	a u8R"(xxx)" a
	b uR"*(xxx)*" b
	c UR"-(xxx)-" c
	d LR"...(xxx)..." d
	---
*/


/* Topic: User-Defined Suffixes
	_____________________________________________

	User-defined suffixes can be declared.  They must start with an underscore.

	--- Code
	a "string"_i18n a
	b u8"string"_abc b
	c R"(string)"_XYZ c
	---
*/