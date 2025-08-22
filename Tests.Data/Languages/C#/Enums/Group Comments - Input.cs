
// Group: Group Comments
// ____________________________________________________________________________
//
// Group comments for enum values aren't supported yet, so the expected behavior for now is
// that these comments are ignored.
//


/* Enum: GroupCommentsA
 */
enum GroupCommentsA
	{
	// Group: Group comment in A
	A, B, C
	}

/* Enum: GroupCommentsB
 */
enum GroupCommentsB
	{
	A, B, C
	// Group: Group comment in B
	}

/* Enum: GroupCommentsC
 */
enum GroupCommentsC
	{
	A,
	B,
	// Group: Group comment in C
	C
	}



// Group: Group Comments (Headerless)
// ____________________________________________________________________________


/** Description
 */
enum GroupComments_HeaderlessA
	{
	// Group: Group comment in A
	A, B, C
	}

/** Description
 */
enum GroupComments_HeaderlessB
	{
	A, B, C
	// Group: Group comment in B
	}

/** Description
 */
enum GroupComments_HeaderlessC
	{
	A,
	B,
	// Group: Group comment in C
	C
	}



// Group: Group Comments (Undocumented)
// ____________________________________________________________________________


enum GroupComments_UndocumentedA
	{
	// Group: Group comment in A
	A, B, C
	}

enum GroupComments_UndocumentedB
	{
	A, B, C
	// Group: Group comment in B
	}

enum GroupComments_UndocumentedC
	{
	A,
	B,
	// Group: Group comment in C
	C
	}
