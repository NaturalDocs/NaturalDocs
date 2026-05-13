
// Struct: SimpleStruct
type SimpleStruct struct {

	// var: Member
	Member int
	}
	
// Struct: ParameterizedStruct
type ParameterizedStruct[T comparable] struct {

	// var: Member
	Member T
	}


// Group: Constraints
// ______________________________________________

// Struct: ConstraintsA
type ConstraintsA [T ~[]E, E any] struct { }

// Struct: ConstraintsB
type ConstraintsB[T OtherType[int]] struct { }

// Struct: ConstraintsC
type ConstraintsC [_ any] struct { }


// Group: Partially Supported Features
// ______________________________________________

// Struct: InterfaceConstraints
// We support constraints using interface{} but currently don't check inside their braces for types.
type InterfaceConstraints[T interface{ ~[]byte|string }] struct { }


// Group: Currently Unsupported Features
// ______________________________________________

// Structs: Parentheses
type (
	A struct { }
	B struct { }
	)
