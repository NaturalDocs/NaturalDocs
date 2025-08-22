
// Group: Stray Comments
// ____________________________________________________________________________
//
// Relocating these comments isn't supported yet, so the expected behavior for now is that
// these comments are ignored.
//


/* Enum: StrayCommentsA
 */
enum StrayCommentsA
	{
	// Topic: Stray comment in A
	A, B, C
	}

/* Enum: StrayCommentsB
 */
enum StrayCommentsB
	{
	A, B, C
	// Topic: Stray comment in B
	}

/* Enum: StrayCommentsC
 */
enum StrayCommentsC
	{
	A,
	B,
	// Topic: Stray comment in C
	C
	}



// Group: Stray Comments (Headerless)
// ____________________________________________________________________________


/** Description
 */
enum StrayComments_HeaderlessA
	{
	// Topic: Stray comment in A
	A, B, C
	}

/** Description
 */
enum StrayComments_HeaderlessB
	{
	A, B, C
	// Topic: Stray comment in B
	}

/** Description
 */
enum StrayComments_HeaderlessC
	{
	A,
	B,
	// Topic: Stray comment in C
	C
	}



// Group: Stray Comments (Undocumented)
// ____________________________________________________________________________


enum StrayComments_UndocumentedA
	{
	// Topic: Stray comment in A
	A, B, C
	}

enum StrayComments_UndocumentedB
	{
	A, B, C
	// Topic: Stray comment in B
	}

enum StrayComments_UndocumentedC
	{
	A,
	B,
	// Topic: Stray comment in C
	C
	}
