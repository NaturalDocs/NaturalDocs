
// Group: Full Comments for Values
// ____________________________________________________________________________


/* Enum: FullCommentsForValues
*/
enum FullCommentsForValues
	{
	// Constant: A
	// Description of A
	A,

	/** Headerless description of B
	*/
	B,

	// Enum: C
	// Enum type for C
	C,

	/// <summary>
	/// XML description of D
	/// </summary>
	D

	// Constant: E
	// Description of E which doesn't appear in code
	}

/** Description
*/
enum FullCommentsForValues_Headerless
	{
	// Constant: A
	// Description of A
	A,

	/** Headerless description of B
	*/
	B,

	// Enum: C
	// Enum type for C
	C,

	/// <summary>
	/// XML description of D
	/// </summary>
	D

	// Constant: E
	// Description of E which doesn't appear in code
	}

enum FullCommentsForValues_Undocumented
	{
	// Constant: A
	// Description of A
	A,

	/** Headerless description of B
	*/
	B,

	// Enum: C
	// Enum type for C
	C,

	/// <summary>
	/// XML description of D
	/// </summary>
	D

	// Constant: E
	// Description of E which doesn't appear in code
	}
