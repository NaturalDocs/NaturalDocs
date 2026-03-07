
public void FunctionA (); /// Description of FunctionA

abstract int FunctionB (int a); /// Description of FunctionB

[Attribute]
protected internal IList<T> FunctionC<T> () where T: System.Collections.IEnumerable; /// Description of FunctionC

static int FunctionD (this string a, int b, params object[] c); /// Description of FunctionD

internal partial void Interface.Interface.FunctionE<T> ()
	where T: new(); /// Description of FunctionE

IEnumerator<T> IEnumerable<T>.GetEnumerator();  /// Description of GetEnumerator
