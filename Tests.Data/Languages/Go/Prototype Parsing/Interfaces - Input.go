
// Interface: SimpleInterface
type SimpleInterface interface {

	// Function: Member
	Member() int { }
	}
	
// Interface: ParameterizedInterface
type ParameterizedInterface[T comparable] interface {

	// Function: Member
	Member() T { }
	}


// Group: Constraints
// ______________________________________________

// Interface: ConstraintsA
type ConstraintsA [T ~[]E, E any] interface { }

// Interface: ConstraintsB
type ConstraintsB[T OtherType[int]] interface { }

// Interface: ConstraintsC
type ConstraintsC [_ any] interface { }


// Group: Partially Supported Features
// ______________________________________________

// Interface: InterfaceConstraints
// We support constraints using interface{} but currently don't check inside their braces for types.
type InterfaceConstraints[T interface{ ~[]byte|string }] interface { }

// Interface: ConstraintLines
// The interface itself can be documented, but the constraint lines don't have a title that could be matched
// to a Natural Docs comment.
type ConstraintLines interface {
	~int | string
	}


// Group: Currently Unsupported Features
// ______________________________________________

// Interfaces: Parentheses
type (
	A interface { }
	B interface { }
	)
