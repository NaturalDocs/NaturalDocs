
/* Topic: Preprocessing Directives
	_____________________________________________

	--- Code
	#if DEBUG
		int x = 12;
	#elseif X || (Y == false)
		int x = 15;
	#endif
	---
*/


// Topic: With Line Comments
// ______________________________________________
//
//	--- Code
//	#define A  // Line comment following
//	#define B  /* Block comment following */
//	---


/* Topic: With Line Breaks
	_____________________________________________

	Line breaks are allowed if they are immediately preceded by a backslash.

	--- Code
	#define TEST(x, y) \
		((x) > (y) ? (x) : (y))
	---
*/


// Topic: With Whitespace
// _____________________________________________
//
// Actual whitespace is the only thing allowed before the # symbol.  Comments aren't allowed.
//
// --- Code
// #define X 0
//    #define y 1
//		#define Z 2
//
// /* comment */ #invalid
// ---
//
// Whitespace is allowed between the # and the directive.
//
// --- Code
// #ifdef X
// #   define X1 1
// #   define X2 2
// #endif
// ---


/* Topic: Traps
	_____________________________________________

	--- Code
	char* x = R"(multiline string
	#this is not a preprocessing directive
	multiline string)";

	/* Multiline comment
	#this is not a preprocessing dirctive
	multiline comment */
	---
*/