
public delegate void DelegateA (int a);

[Attribute]
protected internal delegate IList<T> DelegateB<in T> (params object[] x) 
	where T: System.Collections.IEnumerable, new();
