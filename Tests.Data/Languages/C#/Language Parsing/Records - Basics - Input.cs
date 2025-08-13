
public record WithInheritance : Parent, Interface, Interface2
	{
	}

internal sealed partial record WithModifiers
	{
	}

namespace Namespace
	{
	[Attibute]
	internal protected record WithAttributesAndNamespace
		{
		}
	}

public record AsTemplate<T>
	{
	}

public record AsTemplateWithInheritance<X,Y> : System.Collections.List<Y>, Interface
	{
	}

public record AsTemplateWithConditions<in X, out Y, out Z> 
	where X: class, Interface, new ()
	where Y: System.Collections.IEnumerable<Y>
    where Z: class?, unmanaged, notnull
	{
	}