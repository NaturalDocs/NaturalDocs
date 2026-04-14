
/* Topic: Line Comments
	_____________________________________________

	--- Code
	a // comment
	b /// doc comment
	c //// comment

	d //! doc comment
	e //!! allowed doc comment, but second ! is part of its content
	---
*/


// Topic: Block Comments
// _____________________________________________
//
// --- Code
// a /* comment */ a
// b /** doc comment */ b
// c /*** comment */ c
//
// d /*! doc comment */ d
// e /*!! allowed doc comment, but second ! is part of its content */ e
// ---


// Topic: Nesting Block Comments
// _____________________________________________
//
// All block comment types can nest within each other
//
// --- Code
// a /* level1 /* level2 */ level1 /** level2 */ level1 /*! level2 */ level1 */ a
// b /*! level1 /* level2 */ level1 /** level2 */ level1 /*! level2 */ level1 */ b
// c /** level1 /* level2 */ level1 /** level2 */ level1 /*! level2 */ level1 */ c
//
// d /* level1 /** level2 /*! level3 */ level2 */ level1 */ d
// ---
