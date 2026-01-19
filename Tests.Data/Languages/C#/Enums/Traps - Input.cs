
/* Enum: ExtraCommentTrapsA
 */
enum ExtraCommentTrapsA
	{
	A /** ignore */ = /** ignore */ 10, /** Inline description of A */

	/** ignore */ B = (5 + /** ignore */ 6) /// Inline description of B
	}

/* Enum: ExtraCommentTrapsB
 */
enum ExtraCommentTrapsB
	{
	A /** ignore */ = /** ignore */ 10,

	/** ignore */ B = (5 + /** ignore */ 6)
	}

/* Enum: LineBreakTraps
 */
enum LineBreakTraps
	{
	A =
		2, /** Acceptable description of A */

	B
	= 4, /** Acceptable description of B */

	C = /// Unacceptable because the definition continues
		6,

	D /// Unacceptable because the definition continues
	= 8,

	E = /// Unacceptable because the definition continues
		10, /// Acceptable description of E

	F /// Unacceptable because the definition continues
	= 12, /// Acceptable description of F

	G = 14
		/// Unacceptable description on next line

	}

/* Enum: CommentAfterCloseA
 */
enum CommentAfterCloseA {
	A,
	B,
	C } /// Description of C

/* Enum: CommentAfterCloseB
 */
enum CommentAfterCloseB {
	A,
	B,
	C } /** Description of C */

/* Enum: CommentAfterCloseC
 */
enum CommentAfterCloseC {
	A,
	B,
	C } /// Description of C.
	// Continuation of description of C.

/* Enum: CommentAfterCloseD
 */
enum CommentAfterCloseD {
	A,
	B,
	C } /** Description of C.
	Continuation of description of C. */

/* Enum: MixedCommentTypes
 */
enum MixedCommentTypes {
	A, /// Description of A
	/** Not included because different comment type */

	B /** Description of B */
	/// Not included because different comment type
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
	A, /// Inline description of A
	B, /// Inline description of B
	C  /// Inline descroption of C
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
	B, /// Inline description of B
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
	B, /// Inline description of B
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
	B, /// Inline description of B, but it's not valid to document them this way
	C /// Inline description of C, but it's not valid to document them this way
	}
enum ListEnumB {
	D, /// Inline description of D, but it's not valid to document them this way
	E,
	F /// Inline description of F, but it's not valid to document them this way
	}
enum ListEnumC {
	G, /// Inline description of G, but it's not valid to document them this way
	H, /// Inline description of H, but it's not valid to document them this way
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
	B /// Inline description of B
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
	B, /// First inline description of B
	B, /// Second inline description of B
	C, /// Inline description of C
	C,
	D,
	D /// Inline description of D
	}

/* Enum: DuplicatesInCodeB
 *
 * This isn't valid C# but we still want the parser to not crash on it.
 */
enum DuplicatesInCodeB {
	A,
	A,
	B, /// First inline description of B
	B, /// Second inline description of B
	C, /// Inline description of C
	C,
	D,
	D /// Inline description of D
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
