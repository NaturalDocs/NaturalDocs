
/*
	Topic: Line Comments

		--- code ---

		// Line comment

		real x;  // Line comment
		x = 1.0;  // Line comment

		// These shouldn't be highlighted:
		// real "string" 1.0

		---
*/

//	Topic: Block Comments
//
//		--- code ---
//
//		/* Block comment */
//
//		real x;  /* Block comment */
//
//		x /*bc*/ = /*bc*/ 1.0;
//
//		/* Multiline
//		 * block
//		 * comment
//		 */
//
//		/* These shouldn't be highlighted:
//			real "string" 1.0
//		*/
//
//		---
