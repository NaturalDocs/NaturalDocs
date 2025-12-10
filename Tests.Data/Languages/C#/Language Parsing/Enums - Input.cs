
class TestClass
	{
	public enum EnumA
		{ A, B, C }

	[Attribute]
	public enum EnumB : byte
		{
		A = 0,
		B,
		C = A | B
		}

	public enum EnumC
		{
		A,
		B,
		ExtraCommaAfterLastIsValid,
		}
	}
