
/* Enum: MultilineLineCommentsA
 */
enum MultilineLineCommentsA
	{
	// Leading comment that shouldn't be included.

	A, // Line comment description of A line 1.
		// Line comment description of A line 2.

	B, // Line comment description of B line 1.
		//
		// Line comment description of B line 3, new paragraph.

	C, // Line comment description of C line 1.

		// Non-consecutive comment that shouldn't be included.

	D // Line comment description of D line 1.
		// Line comment description of D line 2.

	// Trailing comment that shouldn't be included.
	}


/* Enum: MultilineLineCommentsB
 */
enum MultilineLineCommentsB
	{
	// Leading comment that shouldn't be included.

	A = 0, // Line comment description of A line 1.
			  // Line comment description of A line 2.

	B = 2, // Line comment description of B line 1.
			  //
			  // Line comment description of B line 3, new paragraph.

	C = 4, // Line comment description of C line 1.

			  // Non-consecutive comment that shouldn't be included.

	D = 6 // Line comment description of D line 1.
			  // Line comment description of D line 2.

	// Trailing comment that shouldn't be included.
	}


/* Enum: MultilineBlockCommentsA
 */
enum MultilineBlockCommentsA
	{
	/* Leading comment that shouldn't be included. */

	A, /* Block comment description of A line 1.
			Block comment description of A line 2. */

	B, /* Block comment description of B line 1.
			Block comment description of B line 2.
		*/

	C, /* Block comment description of C line 1.

			Block comment description of C line 3, new paragraph. */

	D, /* Block comment description of D line 1.

			Block comment description of D line 3, new paragraph.
		*/

	E, /* Block comment description of E line 1.
		 *
		 * Block comment description of E line 3, new paragraph. */

	F, /* Block comment description of F line 1.
		 *
		 * Block comment description of F line 3, new paragraph.
		 */

	G, /* Block comment description of G line 1. */

		/* Non-consecutive comment that shouldn't be included. */

	H, /* Block comment description of H line 1. */
		/* Non-consecutive comment that shouldn't be included. */

	I /* Block comment description of I line 1.
			Block comment description of I line 2. */

	/* Trailing comment that shouldn't be included. */
	}


/* Enum: MultilineBlockCommentsB
 */
enum MultilineBlockCommentsB
	{
	/* Leading comment that shouldn't be included. */

	A = 0, /* Block comment description of A line 1.
				   Block comment description of A line 2. */

	B = 2, /* Block comment description of B line 1.
				   Block comment description of B line 2.
				*/

	C = 4, /* Block comment description of C line 1.

				   Block comment description of C line 3, new paragraph. */

	D = 6, /* Block comment description of D line 1.

				   Block comment description of D line 3, new paragraph.
				*/

	E = 8, /* Block comment description of E line 1.
				*
				* Block comment description of E line 3, new paragraph. */

	F = 10, /* Block comment description of F line 1.
				  *
				  * Block comment description of F line 3, new paragraph.
				  */

	G = 12, /* Block comment description of G line 1. */

				 /* Non-consecutive comment that shouldn't be included. */

	H = 14, /* Block comment description of H line 1. */
				 /* Non-consecutive comment that shouldn't be included. */

	I = 16 /* Block comment description of I line 1.
				   Block comment description of I line 2. */

	/* Trailing comment that shouldn't be included. */
	}


/* Enum: VerticalLinesAndExtraSymbolsA
 */
enum VerticalLinesAndExtraSymbolsA
	{
	A, /* Description of A line 1.
		 * Description of A line 2.
		 * Description of A line 3.
		 */

	B, /** Description of B line 1.
			  Description of B line 2.
			  Description of B line 3.
		  */

	C, /** Description of C line 1.
		  *   Description of C line 2.
		  *   Description of C line 3.
		  */

	D, /** Description of D line 1.
		  ** Description of D line 2.
		  ** Description of D line 3.
		  */

	E, /// Description of E line 1.
		//   Description of E line 2.
		//   Description of E line 3.

	F /// Description of F line 1.
	   /// Description of F line 2.
	   /// Description of F line 3.
	}


/* Enum: VerticalLinesAndExtraSymbolsB
 */
enum VerticalLinesAndExtraSymbolsB
	{
	A, /* Description of A line 1.
		 *
		 * Description of A line 3.
		 */

	B, /** Description of B line 1.

			  Description of B line 3.
		  */

	C, /** Description of C line 1.
		  *
		  *   Description of C line 3.
		  */

	D, /** Description of D line 1.
		  **
		  ** Description of D line 3.
		  */

	E, /// Description of E line 1.
		//
		//   Description of E line 3.

	F /// Description of F line 1.
	   ///
	   /// Description of F line 3.
	}


/* Enum: VerticalLinesAndExtraSymbolsC
 */
enum VerticalLinesAndExtraSymbolsC
	{
	A, /** Description of A. */

	B, /** Description of B.
		  */

	C, /* Description of C. */

	D, /* Description of D.
		  */

	E, /// Description of E.

	F // Description of F.
	}
