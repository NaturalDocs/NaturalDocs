
// Class: WhereClause
class WhereClause<X> 
	where X : struct 
	{  }

// Class: MultipleWhereClauses
class MultipleWhereClauses<X,Y> 
	where X : BaseClass 
	where Y : new(), class 
	{  }

// Class: WhereClausesWithInheritance
class WhereClausesWithInheritance<X,Y> : Parent1<X>, Parent2<Y> 
	where X : A.B.BaseClass, X.Y.Interface
	where Y : new(), class 
	{  }
