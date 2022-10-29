
/** The two parts of C# indexers should *not* be aligned because they are different types.
 */
public int this [int x, int y]
	{
	get
		{ }
	private set
		{ }
	}
