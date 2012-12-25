
class TestClass
	{
	int PropertyA
		{
		get
			{ }
		}

	protected int PropertyB
		{
		get
			{ }
		private set
			{ }
		}

	[Attribute]
	internal System.Text.StringBuilder PropertyC
		{
		[SetAttribute]
		protected internal set
			{  }
		
		[GetAttribute]
		internal get
			{  }
		}

	abstract int PropertyD
		{  get;  set;  }
	}
