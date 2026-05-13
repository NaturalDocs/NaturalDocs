
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


// Section: Constraints
// ______________________________________________

// Struct: ConstraintsA
type ConstraintsA [T ~[]E, E any] struct { }

// Struct: ConstraintsB
type ConstraintsB[T OtherType[int]] struct { }

// Struct: ConstraintsC
type ConstraintsC [_ any] struct { }


// Section: Partially Supported Features
// ______________________________________________

// Struct: InterfaceConstraints
// We support constraints appearing inside interface{} but currently don't parse inside their braces for types.
type InterfaceConstraints[T interface{ ~[]byte|string }] struct { }


// Section: Currently Unsupported Features
// ______________________________________________

// Structs: Parentheses
type (
	A struct { }
	B struct { }
	)
