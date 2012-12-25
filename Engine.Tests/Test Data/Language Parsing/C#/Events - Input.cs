
class TestClass
	{
	public event Delegate EventA;

	[Attribute]
	private event Delegate EventB, EventC;

	internal event Delegate EventD
		{
		add { }
		remove { }
		}

	[Attribute]
	internal event Delegate EventE
		{
		[AddAttribute] add { }
		[RemoveAttribute] remove { }
		}
	}