
public record WithEmptyParameters ()
	{
	}

public record WithEmptyParameters_NoBody ( );

// This will show two (No class prototype detected) entries because it creates properties for the two parameters.
public record WithParameters (string X, int Y)
	{
	}

// This will show two (No class prototype detected) entries because it creates properties for the two parameters.
public record WithParameters_NoBody (string X, int Y);

// This will show one (No class prototype detected) entry because it creates a property for the Z parameter.
public record WithParametersAndInheritance (string X, int Y, float Z)
	: Parent (X, Y)
	{
	}

// This will show one (No class prototype detected) entry because it creates a property for the Z parameter.
public record WithParametersAndInheritance_NoBody (string X, int Y, float Z)
	: Parent (X, Y);

// This will show three (No class prototype detected) entries because it creates properties for the two parameters plus Z.
public record WithParametersAndMembers (string X, int Y)
	{
	public float Z { get; }
	}

// This will show three (No class prototype detected) entries because it creates properties for the three parameters.
public record WithParametersAndModifiers (Namespace.MyClass X_2, [attribute] int Y, in float Z = 1.2);

// This will show three (No class prototype detected) entries because it creates properties for the Z parameters plus Q and MyFunction().
public record WithAll<T> (Namespace.MyClass X_2, 
									[attribute] int Y, 
									in float Z = 1.2)
	: Parent (X_2), Parent2 (in Y), Parent3
    where T: unmanaged, notnull
	{
	public List<T> Q { get; }
	public void MyFunction() { }
	}