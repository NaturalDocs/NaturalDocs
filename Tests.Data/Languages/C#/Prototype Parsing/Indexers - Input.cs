
int this [int x]
	{
	get
		{ }
	}

protected int this [int x]
	{
	get
		{ }
	private set
		{ }
	}

[Attribute]
internal System.Text.StringBuilder this [int x, int y]
	{
	[SetAttribute]
	protected internal set
		{  }

	[GetAttribute]
	internal get
		{  }
	}

abstract int this [int x]
	{  get;  set;  }

public int this [int x] => x ? 1 : 0;

public int this [int x]
	{
	get => array[x];
	set => array[x] = value;
	}
