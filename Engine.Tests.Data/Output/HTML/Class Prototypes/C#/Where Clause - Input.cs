
class WhereClause<X>
	where X : struct
	{  }

class MultipleWhereClauses<X,Y, out Z>
	where X : BaseClass
	where Y : new(), class
    where Z : class?, unmanaged, notnull
	{  }

public static class WhereClausesWithInheritance<X,Y> : Parent1<X>, Parent2<Y>
	where X : A.B.BaseClass<X>, X.Y.Interface
	where Y : new(), class
	{  }
