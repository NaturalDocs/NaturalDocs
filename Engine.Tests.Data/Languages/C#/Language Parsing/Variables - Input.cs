
class TestClass
	{
	int varA;

	public int varB = 12;

	private static List<int> varC = null;

	volatile string[] varD = { "a", "b", "don't trip on this };" };

	[Attribute: Something("value")]
	int* varE;

	[AttributeA][AttributeB(12)]
	protected internal System.Text.StringBuilder varF, varG = null, varH;

	int* varI, varJ;

	void* varK, varL;

	delegate*<int, int> varM;

	delegate* managed<float> varN;

	delegate* unmanaged<int> varO;

	delegate* unmanaged[Cdecl] <int, float> varP;

	public int PropertyNotVariableA => 12;

	public int PropertyNotVariableB => x ? 0 : 12;
	}
