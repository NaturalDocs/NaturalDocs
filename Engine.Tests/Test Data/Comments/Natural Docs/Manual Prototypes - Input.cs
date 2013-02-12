
/* Manual Prototype Removal
	___________________________________________________________________________

*/


// The topic below has no body except for the prototype.  The body should end up completely empty.

/*	Function: NoBody1

	(prototype)
		void NoBody1 ()
	(end)
*/


// If the only content other than the prototype is a heading, the body should end up completely empty.

/*	Function: NoBody2

	Heading:

		(prototype)
			void NoBody2 ()
		(end)
*/

/* Function: ExtraContentAbove1

	Since there's content in this topic before the prototype, it should remain in the output.

	(prototype)
		void ExtraContentAbove1 ()
	(end)
*/

/* Function: ExtraContentAbove2

	- The same should apply for non-paragraph formatting like bullet lists.

	(prototype)
		void ExtraContentAbove2 ()
	(end)
*/

/* Function: ExtraContentBelow1

	(prototype)
		void ExtraContentBelow1 ()
	(end)

	Since there's content in this topic after the prototype, it should remain in the output.
*/

/* Function: ExtraContentBelow2

	(prototype)
		void ExtraContentBelow2 ()
	(end)

	Blah - The same should apply for non-paragraph formatting like definition lists.
*/

/* Function: HeadingAboveRemoved

	Heading Above:

		(prototype)
			void HeadingAboveRemoved ()
		(end)

	Heading Below:

		Since there's no content underneath "Heading Above" after the manual prototype is removed,
		the heading should be removed as well.  "Heading Below" should remain.
*/

/* Function: HeadingAboveStays

	Heading Above:

		(prototype)
			void HeadingAboveStays ()
		(end)

		Since there's content undernead "Heading Above" after the manual prototype is removed, it
		should remain in the output.
*/