
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
	A, /// Inline description of A
	B, /// Inline description of B
	C  /** Inline description of C */
	}

/* Enum: DescriptionsInlineB
 */
enum DescriptionsInlineB
	{
	A = 10, /// Inline description of A
	B = (5 + 6), /// Inline description of B
	C  /** Inline description of C */
	}

/* Enum: DescriptionsInlineC
 */
[Flags]
enum DescriptionsInlineC : byte
	{
	A = 0x01, /// Inline description of A
	B, /// Inline description of B
	C  /** Inline description of C */
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
	A, /// Inline description of A
	B, /// Inline description of B
	C  /** Inline description of C */
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
	A = 10, /// Inline description of A
	B = (5 + 6), /// Inline description of B
	C  /** Inline description of C */
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
	A = 0x01, /// Inline description of A
	B, /// Inline description of B
	C  /** Inline description of C */
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
	B, /// Inline description of B
	C  /** Inline description of C */
	}

/* Enum: MixedDescriptionsB
 *
 * Values:
 *    C - Comment description of C
 */
enum MixedDescriptionsB
	{
	A = 10, /// Inline description of A
	B = (5 + 6), /// Inline description of B
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
	B, /// Inline description of B
	C  /** Inline description of C */
	}

