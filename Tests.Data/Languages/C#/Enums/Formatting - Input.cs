
/* Enum: InlineFormatting
 *
 * Values:
 *    A - Comment description of A with *bold* and _underline_ and
 *			 email@addresses.com and <https://www.naturaldocs.org> and
 *			 <named links: https://www.naturaldocs.org>.
 */
enum InlineFormatting {
	A,
	B, /// Inline description of B with *bold* and _underline_ and
	   // email@addresses.com and <https://www.naturaldocs.org> and
	   // <named links: https://www.naturaldocs.org>.
	C /** Inline description of C with *bold* and _underline_ and
	   email@addresses.com and <https://www.naturaldocs.org> and
	   <named links: https://www.naturaldocs.org>. */
	}


/* Enum: BlockFormatting_FullComments
*/
enum BlockFormatting_FullComments
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


/* Enum: BlockFormatting_InlineComments
*/
enum BlockFormatting_InlineComments
	{
	A, /// Paragraph before bullet list
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

	B, /// Paragraph before definition list
		//
		// Definition1 - Description of definition 1
		//
		// Definition2 - Description of definition 2
		//
		//   Description of definition 2 second paragraph
		//
		// Paragraph after definition list

	C, /// Paragraph before heading
		//
		// Heading:
		//
		// Paragraph after heading

	D /// Paragraph before code block
		//
		// --- Code ---
		// int x = 12;
		// ---
		//
		// Paragraph after code block
	}
