
// The global properties should not be included with the class even though they are directly above it
// along with its other properties.

[assembly: GlobalAttribute]
[module: GlobalAttribute2("don't trip on this: )]")]
[UntargetedAttribute]
[UntargetedAttribute2("don't trip on this: )]", false)]
class TestClass
	{

	[method: MethodAttribute]
	[return: ReturnAttribute("don't trip on this: )]")]
	public int TestFunction (int x, [param: something("don't trip on this: )]){")][AnotherOne] int y)
		{
		return 0;
		}

	[Attribute1]
	[Attribute2]
	// Comment which shouldn't be included with the enum.
	[Attribute3]
	public enum TestEnum
		{ X, Y, Z }

	}
