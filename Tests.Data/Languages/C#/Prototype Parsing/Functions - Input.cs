
void Parameterless ()
	{
	}

void Basic (int x, string y)
	{
	}

public static string Modifiers (int? x, out string y, in MyStruct z)
	{
	}

public async int AsyncModifier ()
	{
	}

protected internal void CompoundModifierA ()
	{
	}

private protected void CompoundModifierB ()
	{
	}

[Attribute][DllImport ("NaturalDocs.Engine.SQLite.dll")]
extern static private bool Attributes_NoBraces ([MarshalAs(UnmanagedType.LPStr)] string x,
															  [param: something("don't trip on this: )]){")][AnotherOne] int y,
															  out int z);

void DefaultValues (string x = "),;[<{ \" ),;[<{", int[,,] y = [1,2,3], int z = 6)
	{
	}

public List<int> Templates (System.Collections.Generic.Dictionary<Nullable<int>, Map<string, Object[]>> x = null,
									 int y = 12)
	{
	}

public List<T1> WhereClause<T1,T2> (List<T2> x)
	where T1: class, Interface, new ()
	where T2: System.Collections.IEnumerable<T2>
	{
	}

public int ExpressionBodyA (bool x) => x ? 1 : 0;

public static RgbColor ExpressionBodyB (RgbColor color) =>
	new RgbColor(
		color.Red ^ 0xFF,
		color.Green ^ 0xFF,
		color.Blue ^ 0xFF
		);

public void FunctionPointer (delegate* unmanaged[Cdecl] <int, float> x)
	{
	}

static ref int MultipleParameterModifiers<T>(this ref int x, T y) where T:unmanaged, Enum
	{
	}

public void NullableWithOtherModifiers (int[]? a,
														  int?[] b,
														  int?[,,]?[]? c,
														  List<T>? d,
														  List<T?> e,
														  List<T?>?[]? f,
														  List<T?[,,]?>?[]? g)
	{
	}
