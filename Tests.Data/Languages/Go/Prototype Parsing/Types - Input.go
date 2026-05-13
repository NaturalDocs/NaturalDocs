
// Type: SimpleType
type SimpleType int32

// Type: ParameterizedType
type ParameterizedType[P any] x[P]

// Type: SimpleAlias
type SimpleAlias = int32

// Type: ParameterizedAlias
type ParameterizedAlias[P comparable] = x[P]


// Group: Constraints
// ______________________________________________

// Type: ConstraintsA
type ConstraintsA [T ~[]E, E any] OtherType[T]

// Type: ConstraintsB
type ConstraintsB[T OtherType[int]] OtherType[T]

// Type: ConstraintsC
type ConstraintsC [_ any] OtherType


// Group: Partially Supported Features
// ______________________________________________

// Type: InterfaceConstraints
// We support constraints using interface{} but currently don't check inside their braces for types.
type InterfaceConstraints[T interface{ ~[]byte|string }] OtherType[T]


// Group: Currently Unsupported Features
// ______________________________________________

// Types: Parentheses
type (
	A = int64
	B = float32
	)
