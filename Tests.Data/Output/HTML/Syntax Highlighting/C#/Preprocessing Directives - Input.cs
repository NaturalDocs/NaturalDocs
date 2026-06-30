
//	Topic: Preprocessing Directives
//
//		--- Code
//		#if DEBUG
//			int x = 12;
//		#elseif X || (Y == false)
//			int x = 15;
//		#endif
//
//		#define A  // Line comments are allowed afterwards
//
//		#define B  /* Block comments are not */
//		---

// Topic: Traps
//
//		--- Code
//		string x = @"Multiline string
//		#define THIS_IS_INVALID
//		multiline string";
//
//		/* Multiline comment
//		#define THIS_IS_INVALID
//		multiline comment */
//		---
