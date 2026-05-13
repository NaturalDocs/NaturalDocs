
// Constant: Typed
const Typed float64 = 1.23

// Constant: Untyped
const Untyped = 0.0

// Constant: ParameterizedType
const ParameterizedType TypeName[int] = nil


// Group: Currently Unsupported Features
// ______________________________________________

// Constants: Parentheses
const (
	A int64 = 1024
	B = -1
)

// Constants: Multiple Assignments 1
const A, B, C = 3, 4, "foo"

// Constants: Multiple Assignments 2
// Both constants are float32
const A, B float32 = 0, 3