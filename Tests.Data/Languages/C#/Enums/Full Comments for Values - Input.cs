
// Group: Full Comments for Values
// ____________________________________________________________________________
//
// Properly merging these comments isn't supported yet, so the expected behavior for now is
// that these comments are ignored.
//

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



// Group: Formatting
// ____________________________________________________________________________


/* Enum: BlockFormatting
*/
enum BlockFormatting
	{
	// Constant: A
	//
	// Paragraph before bullet list
	//
	// - Bullet 1
	//
	// - Bullet 2
	//
	//   Bullet 2 second paragraph
	//
	//   - Bullet 3 level 2
	//
	// Paragraph after bullet list
	A,

	// Constant: B
	//
	// Paragraph before definition list
	//
	// Definition1 - Description of definition 1
	//
	// Definition2 - Description of definition 2
	//
	//   Description of definition 2 second paragraph
	//
	// Paragraph after definition list
	B,

	// Constant: C
	//
	// Paragraph before heading
	//
	// Heading:
	//
	// Paragraph after heading
	C,

	// Constant: D
	//
	// Paragraph before code block
	//
	// --- Code ---
	// int x = 12;
	// ---
	//
	// Paragraph after code block
	D
	}
