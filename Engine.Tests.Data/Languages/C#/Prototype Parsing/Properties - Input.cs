
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

public int PropertyE => x ? 1 : 0;

public string PropertyF => string.Format("#{0:X2}{1:X2}{2:X2}", Red, Green, Blue);

public int PropertyG
	{
	get => x;
	set => x = value;
	}
