
class TestClass
	{
	public static TestClass operator ! (TestClass input)
		{ }

	public static TestClass operator++ (TestClass input)
		{ }

	public static extern bool operator true (TestClass input);

	public static TestClass operator+ (TestClass a, TestClass b)
		{ }

	[Attribute]
	public static TestClass<T> operator<< (TestClass<T> a, TestClass<T> b)
		{ }

	public static extern implicit operator string (TestClass input);

	public static explicit operator List<string> (TestClass input)
		{ }
	}
