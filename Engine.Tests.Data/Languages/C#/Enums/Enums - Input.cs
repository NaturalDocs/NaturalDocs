
// Group: No Descriptions
// ____________________________________________________________________________


/* Enum: NoDescriptionsA
 */
enum NoDescriptionsA
	{  A, B, C  }

/* Enum: NoDescriptionsB
 */
enum NoDescriptionsB
	{  A = 10, B = (5 + 6), C  }

/* Enum: NoDescriptionsC
 */
[Flags]
enum NoDescriptionsC : byte
	{  A = 0x01, B, C  }



// Group: Descriptions in Comment
// ____________________________________________________________________________


/* Enum: DescriptionsInCommentA
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
enum DescriptionsInCommentA
	{  A, B, C  }

/* Enum: DescriptionsInCommentB
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
enum DescriptionsInCommentB
	{  A = 10, B = (5 + 6), C  }

/* Enum: DescriptionsInCommentC
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
[Flags]
enum DescriptionsInCommentC : byte
	{  A = 0x01, B, C  }



// Group: Descriptions Inline
// ____________________________________________________________________________


/* Enum: DescriptionsInlineA
 */
enum DescriptionsInlineA
	{
	A, // Inline description of A
	B, // Inline description of B
	C  /* Inline description of C */
	}

/* Enum: DescriptionsInlineB
 */
enum DescriptionsInlineB
	{
	A = 10, // Inline description of A
	B = (5 + 6), // Inline description of B
	C  /* Inline description of C */
	}

/* Enum: DescriptionsInlineC
 */
[Flags]
enum DescriptionsInlineC : byte
	{
	A = 0x01, // Inline description of A
	B, // Inline description of B
	C  /* Inline description of C */
	}



// Group: Descriptions in Both
// ____________________________________________________________________________


/* Enum: DescriptionsInBothA
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
enum DescriptionsInBothA
	{
	A, // Inline description of A
	B, // Inline description of B
	C  /* Inline description of C */
	}

/* Enum: DescriptionsInBothB
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
enum DescriptionsInBothB
	{
	A = 10, // Inline description of A
	B = (5 + 6), // Inline description of B
	C  /* Inline description of C */
	}

/* Enum: DescriptionsInBothC
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
[Flags]
enum DescriptionsInBothC : byte
	{
	A = 0x01, // Inline description of A
	B, // Inline description of B
	C  /* Inline description of C */
	}



// Group: Mixed Descriptions
// ____________________________________________________________________________


/* Enum: MixedDescriptionsA
 *
 * Values:
 *    A - Comment description of A
 */
enum MixedDescriptionsA
	{
	A,
	B, // Inline description of B
	C  /* Inline description of C */
	}

/* Enum: MixedDescriptionsB
 *
 * Values:
 *    C - Comment description of C
 */
enum MixedDescriptionsB
	{
	A = 10, // Inline description of A
	B = (5 + 6), // Inline description of B
	C
	}

/* Enum: MixedDescriptionsC
 *
 * Values:
 *    A - Comment description of A
 *    C - Comment description of C
 */
[Flags]
enum MixedDescriptionsC : byte
	{
	A = 0x01,
	B, // Inline description of B
	C  /* Inline description of C */
	}



// Group: Multiline Descriptions
// ____________________________________________________________________________


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



// Group: No Comments
// ____________________________________________________________________________


enum NoCommentsA
	{  A, B, C  }

enum NoCommentsB
	{
	A, // Inline description of A
	B, // Inline description of B
	C  /* Inline description of C */
	}

[Flags]
enum NoCommentsC : byte
	{
	A = 10, // Inline description of A
	B = (5 + 6),
	C  /* Inline description of C */
	}

enum EmptyInlineComments {
	A, //
	B, /**/
	C, /* */
	D, //
	//
	E /*
	*/
	}



// Group: Formatting
// ____________________________________________________________________________


/* Enum: BasicFormatting
 *
 * Values:
 *    A - Comment description of A with *bold* and _underline_ and
 *			 email@addresses.com and <https://www.naturaldocs.org> and
 *			 <named links: https://www.naturaldocs.org>.
 */
enum BasicFormatting {
	A,
	B, // Inline description of B with *bold* and _underline_ and
	   // email@addresses.com and <https://www.naturaldocs.org> and
	   // <named links: https://www.naturaldocs.org>.
	C /* Inline description of C with *bold* and _underline_ and
	   email@addresses.com and <https://www.naturaldocs.org> and
	   <named links: https://www.naturaldocs.org>. */
	}



// Group: Headerless Comments
// ____________________________________________________________________________


/**
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 */
enum HeaderlessComments {
	A,
	B, // Inline description of B
	C /* Inline description of C */
	}



// Group: Traps
// ____________________________________________________________________________


/* Enum: ExtraCommentTrapsA
 */
enum ExtraCommentTrapsA
	{
	A /* ignore */ = /* ignore */ 10, /* Inline description of A */

	/* ignore */ B = (5 + /* ignore */ 6) // Inline description of B
	}

/* Enum: ExtraCommentTrapsB
 */
enum ExtraCommentTrapsB
	{
	A /* ignore */ = /* ignore */ 10,

	/* ignore */ B = (5 + /* ignore */ 6)
	}

/* Enum: LineBreakTraps
 */
enum LineBreakTraps
	{
	A =
		2, /* Acceptable description of A */

	B
	= 4, /* Acceptable description of B */

	C = // Unacceptable because the definition continues
		6,

	D // Unacceptable because the definition continues
	= 8,

	E = // Unacceptable because the definition continues
		10, // Acceptable description of E

	F // Unacceptable because the definition continues
	= 12, // Acceptable description of F

	G = 14
		// Unacceptable description on next line

	}

/* Enum: CommentAfterCloseA
 */
enum CommentAfterCloseA {
	A,
	B,
	C } // Description of C

/* Enum: CommentAfterCloseB
 */
enum CommentAfterCloseB {
	A,
	B,
	C } /* Description of C */

/* Enum: CommentAfterCloseC
 */
enum CommentAfterCloseC {
	A,
	B,
	C } // Description of C.
	// Continuation of description of C.

/* Enum: CommentAfterCloseD
 */
enum CommentAfterCloseD {
	A,
	B,
	C } /* Description of C.
	Continuation of description of C. */

/* Enum: MixedCommentTypes
 */
enum MixedCommentTypes {
	A, // Description of A
	/* Not included because different comment type */

	B /* Description of B */
	// Not included because different comment type
	}

/* Enum: ExtraInCode_InOrderA
 *
 * Values:
 *    B - Description of B
 *    C - Description of C
 */
enum ExtraInCode_InOrderA {
	A, B, C
	}

/* Enum: ExtraInCode_InOrderB
 *
 * Values:
 *    A - Description of A
 *    B - Description of B
 */
enum ExtraInCode_InOrderB {
	A, B, C
	}

/* Enum: ExtraInCode_InOrderC
 *
 * Values:
 *    A - Description of A
 *    C - Description of C
 */
enum ExtraInCode_InOrderC {
	A, B, C
	}

/* Enum: ExtraInCode_InOrderD
 *
 * Values:
 *    C - Description of C
 *    F - Description of F
 *    H - Description of H
 *    I - Description of I
 */
enum ExtraInCode_InOrderD {
	A, B, C, D, E, F, G, H, I, J, K
	}

/* Enum: ExtraInCode_OutOfOrderA
 *
 * Values:
 *    B - Description of B
 *    A - Description of A
 */
enum ExtraInCode_OutOfOrderA {
	A, B, C, D
	}

/* Enum: ExtraInCode_OutOfOrderB
 *
 * Values:
 *    A - Description of A
 *    B - Description of B
 */
enum ExtraInCode_OutOfOrderB {
	D, C, B, A
	}

/* Enum: ExtraInCode_OutOfOrderC
 *
 * Values:
 *    C - Description of C
 *    B - Description of B
 */
enum ExtraInCode_OutOfOrderC {
	A, B, C, D
	}

/* Enum: ExtraInComment
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 *    D - Comment description of non-existent D
 */
enum ExtraInComment {
	A, // Inline description of A
	B, // Inline description of B
	C  // Inline descroption of C
	}

/* Enum: ContentSurroundingList_InOrder
 *
 * This paragraph appears before the list.
 *
 * A - Comment description of A
 * C - Comment description of C
 *
 * This paragraph appears after the list.
 */
enum ContentSurroundingList_InOrder {
	A,
	B, // Inline description of B
	C
	}

/* Enum: ContentSurroundingList_OutOfOrder
 *
 * This paragraph appears before the list.
 *
 * C - Comment description of C
 * A - Comment description of A
 *
 * This paragraph appears after the list.
 */
enum ContentSurroundingList_OutOfOrder {
	A,
	B, // Inline description of B
	C
	}

/* Enums: ListTopic
 *
 * ListEnumA - Comment description of ListEnumA
 * ListEnumB - Comment description of ListEnumB
 * ListEnumC - Comment description of ListEnumC
 *
 * Values:
 *    A - Comment description of A, but it's not valid to document them this way
 *    B - Comment description of B, but it's not valid to document them this way
 *    E - Comment description of E, but it's not valid to document them this way
 *    F - Comment description of F, but it's not valid to document them this way
 *    G - Comment description of G, but it's not valid to document them this way
 *    I - Comment description of I, but it's not valid to document them this way
 */
enum ListEnumA {
	A,
	B, // Inline description of B, but it's not valid to document them this way
	C // Inline description of C, but it's not valid to document them this way
	}
enum ListEnumB {
	D, // Inline description of D, but it's not valid to document them this way
	E,
	F // Inline description of F, but it's not valid to document them this way
	}
enum ListEnumC {
	G, // Inline description of G, but it's not valid to document them this way
	H, // Inline description of H, but it's not valid to document them this way
	I
	}

/* Enum: DuplicatesInComment
 *
 * Values:
 *    A - First comment description of A
 *    A - Second comment description of A
 *    B - First comment description of B
 *    B - Second comment description of B
 */
enum DuplicatesInComment {
	A,
	B // Inline description of B
	}

/* Enum: DuplicatesInCodeA
 *
 * This isn't valid C# but we still want the parser to not crash on it.
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 *    D - Comment description of D
 */
enum DuplicatesInCodeA {
	A,
	A,
	B, // First inline description of B
	B, // Second inline description of B
	C, // Inline description of C
	C,
	D,
	D // Inline description of D
	}

/* Enum: DuplicatesInCodeB
 *
 * This isn't valid C# but we still want the parser to not crash on it.
 */
enum DuplicatesInCodeB {
	A,
	A,
	B, // First inline description of B
	B, // Second inline description of B
	C, // Inline description of C
	C,
	D,
	D // Inline description of D
	}

/* Enum: MissingValuesA
 *
 * This isn't valid C# but we still want the parser to not crash on it.
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
enum MissingValuesA {,A,,B,C,}

/* Enum: MissingValuesB
 *
 * This isn't valid C# but we still want the parser to not crash on it.
 */
enum MissingValuesB {,A,,B,C,}

/* Enum: NoDefinitionA
 *
 * This is valid, we just want to make sure the parser handles it.
 */

/* Enum: NoDefinitionB
 *
 * This is valid, we just want to make sure the parser handles it.
 *
 * Values:
 *    A - Comment description of A
 *    B - Comment description of B
 *    C - Comment description of C
 */
