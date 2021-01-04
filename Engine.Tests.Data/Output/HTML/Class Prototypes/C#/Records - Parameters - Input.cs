
public record WithEmptyParameters ()
	{
	}

public record WithEmptyParameters_NoBody ( );

public record WithParameters (string X, int Y)
	{
	}

public record WithParameters_NoBody (string X, int Y);

public record WithParametersAndInheritance (string X, int Y, float Z)
	: Parent (X, Y)
	{
	}

public record WithParametersAndInheritance_NoBody (string X, int Y, float Z)
	: Parent (X, Y);

public record WithParametersAndMembers (string X, int Y)
	{
	public float Z { get; }
	}

public record WithParametersAndModifiers (Namespace.MyClass X_2, [attribute] int Y, in float Z = 1.2);

public record WithAll<T> (Namespace.MyClass X_2, 
									[attribute] int Y, 
									in float Z = 1.2)
	: Parent (X_2), Parent2 (in Y), Parent3
    where T: unmanaged, notnull
	{
	public List<T> Q { get; }
	public void MyFunction() { }
	}