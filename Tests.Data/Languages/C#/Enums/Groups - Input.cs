
// Group: Groups in Code
// ____________________________________________________________________________


enum GroupsInCodeA
	{
	// Group: Group at Start
	A,
	B,
	// Group: Group at Middle
	C,
	D
	}

enum GroupsInCodeB
	{
	A,
	B,
	// Group: Group at Middle Only
	C,
	D
	}

enum GroupsInCodeC
	{
	// Group: Group at Start
	A, /// Description of A
	B, /// Description of B

	// Group: Group at Middle
	C, /// Description of C
	D /// Description of D
	}


enum GroupsInCodeD
	{
	A, /// Description of A
	B, /// Description of B

	// Group: Group at Middle Only
	C, /// Description of C
	D /// Description of D
	}



// Group: Groups in Comment
// ____________________________________________________________________________


/* Enum: GroupsInCommentA
 *
 * Group at Start:
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle:
 *    C - Description of C
 *    D - Description of D
 */
enum GroupsInCommentA
	{
	A,
	B,
	C,
	D
	}

/* Enum: GroupsInCommentB
 *
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle Only:
 *    C - Description of C
 *    D - Description of D
 */
enum GroupsInCommentB
	{
	A,
	B,
	C,
	D
	}

/**
 * Group at Start:
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle:
 *    C - Description of C
 *    D - Description of D
 */
enum GroupsInCommentC
	{
	A,
	B,
	C,
	D
	}



// Group: Extra Values in Code, In Order
// ____________________________________________________________________________


/* Enum: ExtraValuesInCodeA_InOrder
 *
 * Group at Start:
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle:
 *    C - Description of C
 *    D - Description of D
 */
enum ExtraValuesInCodeA_InOrder
	{
	A,
	B,
	C,
	D,
	E
	}

/* Enum: ExtraValuesInCodeB_InOrder
 *
 * Group at Start:
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle:
 *    D - Description of D
 *    E - Description of E
 */
enum ExtraValuesInCodeB_InOrder
	{
	A,
	B,
	C,
	D,
	E
	}

/* Enum: ExtraValuesInCodeC_InOrder
 *
 * Group at Start:
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle:
 *    E - Description of E
 *    F - Description of F
 */
enum ExtraValuesInCodeC_InOrder
	{
	A,
	B,
	C,
	D,
	E,
	F,
	G
	}

/* Enum: ExtraValuesInCodeD_InOrder
 *
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle Only:
 *    C - Description of C
 *    D - Description of D
 */
enum ExtraValuesInCodeD_InOrder
	{
	A,
	B,
	C,
	D,
	E
	}

/* Enum: ExtraValuesInCodeE_InOrder
 *
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle Only:
 *    D - Description of D
 *    E - Description of E
 */
enum ExtraValuesInCodeE_InOrder
	{
	A,
	B,
	C,
	D,
	E
	}

/* Enum: ExtraValuesInCodeF_InOrder
 *
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle Only:
 *    E - Description of E
 *    F - Description of F
 */
enum ExtraValuesInCodeF_InOrder
	{
	A,
	B,
	C,
	D,
	E,
	F,
	G
	}



// Group: Extra Values in Code, Out of Order
// ____________________________________________________________________________


/* Enum: ExtraValuesInCodeA_OutOfOrder
 *
 * Group at Start:
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle:
 *    C - Description of C
 *    D - Description of D
 */
enum ExtraValuesInCodeA_OutOfOrder
	{
	E,
	D,
	C,
	B,
	A
	}

/* Enum: ExtraValuesInCodeB_OutOfOrder
 *
 * Group at Start:
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle:
 *    D - Description of D
 *    E - Description of E
 */
enum ExtraValuesInCodeB_OutOfOrder
	{
	E,
	D,
	C,
	B,
	A
	}

/* Enum: ExtraValuesInCodeC_OutOfOrder
 *
 * Group at Start:
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle:
 *    E - Description of E
 *    F - Description of F
 */
enum ExtraValuesInCodeC_OutOfOrder
	{
	G,
	F,
	E,
	D,
	C,
	B,
	A
	}

/* Enum: ExtraValuesInCodeD_OutOfOrder
 *
 *    A - Description of A
 *    B - Description of B
 *
 * Group at Middle Only:
 *    C - Description of C
 *    D - Description of D
 */
enum ExtraValuesInCodeD_OutOfOrder
	{
	E,
	D,
	C,
	B,
	A
	}

/* Enum: ExtraValuesInCodeE_OutOfOrder
 *
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle Only:
 *    D - Description of D
 *    E - Description of E
 */
enum ExtraValuesInCodeE_OutOfOrder
	{
	E,
	D,
	C,
	B,
	A
	}

/* Enum: ExtraValuesInCodeF_OutOfOrder
 *
 *    B - Description of B
 *    C - Description of C
 *
 * Group at Middle Only:
 *    E - Description of E
 *    F - Description of F
 */
enum ExtraValuesInCodeF_OutOfOrder
	{
	G,
	F,
	E,
	D,
	C,
	B,
	A
	}



// Group: Extra Values in Comment
// ____________________________________________________________________________


/* Enum: ExtraValuesInCommentA
 *
 * Group 1:
 *    A - Description of A
 *    B - Description of B
 *
 * Group 2:
 *    C - Description of C
 *    D - Description of D
 */
enum ExtraValuesInCommentA
	{
	B,
	C
	}



// Group: Formatting
// ____________________________________________________________________________


enum GroupsWithDescriptions
	{
	// Group: Group at Start
	// Plain text description with group at start.

	A, /// Description of A
	B, /// Description of B

	// Group: Group at Middle 1
	//
	// Description in group at middle with definition list.
	//
	// Item1 - Definition of Item1
	// Item2 - Definition of Item2

	C, /// Description of C
	D, /// Description of D

	// Group: Group at Middle 2
	//
	// Description in group at middle with bullet list.
	//
	// - Bullet 1
	// - Bullet 2

	E, /// Description of E
	F /// Description of F
	}


/* Enum: DocumentationButNoValuesA
 * A description of DocumentationButNoValuesA.
 */
enum DocumentationButNoValuesA
	{
	// Group: Group at Start
	A,
	B, /// Description of B

	// Group: Group at Middle With Description
	// Description in group at middle.

	C, /// Description of C
	D
	}


/* Enum: DocumentationButNoValuesB
 * A description of DocumentationButNoValuesB.
 */
enum DocumentationButNoValuesB
	{
	A, /// Description of A
	B,

	// Group: Group at Middle Only

	C,
	D /// Description of D
	}



// Group: Traps
// ____________________________________________________________________________


enum GroupAfterLastInCode
	{
	A,
	B,
	C
	// Group: Group after last
	}

/* Enum: GroupAfterLastInComment

	A - Description of A
	B - Description of B
	C - Description of C

	Group 1:
*/
enum GroupAfterLastInComment
	{
	A,
	B,
	C
	}

/* Enum: GroupsInBothA

	Group in Comment at Start:

	A - Description of A
	B - Description of B

	Group in Comment at Middle:

	C - Description of C
	D - Description of D
*/
enum GroupsInBothA
	{
	// Group: Group in Code at Start
	A,
	B,
	// Group: Group in Code at Middle
	C,
	D
	}

/* Enum: GroupsInBothB

	A - Description of A
	B - Description of B

	Group in Comment at Middle Only:

	C - Description of C
	D - Description of D
*/
enum GroupsInBothB
	{
	// Group: Group in Code at Start
	A,
	B,
	// Group: Group in Code at Middle
	C,
	D
	}

/* Enum: GroupsInBothC

	A - Description of A
	B - Description of B
	C - Description of C
	D - Description of D
*/
enum GroupsInBothC
	{
	// Group: Group in Code at Start
	A,
	B,
	// Group: Group in Code at Middle
	C,
	D
	}

/* Enum: GroupsInBothD

	Group in Comment at Start:

	A - Description of A
	B - Description of B

	Group in Comment at Middle:

	D - Description of D
	E - Description of E
*/
enum GroupsInBothD
	{
	// Group: Group in Code at Start
	A,
	B,
	C,
	// Group: Group in Code at Middle
	D,
	E,
	F
	}
