
void Parameterless ()
	{
	}

void Basic (int x, string y)
	{
	}

public static string Modifiers (int? x, out string y)
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
