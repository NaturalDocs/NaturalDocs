
// Group: Basic Types
// ______________________________________________

// Variable: DeclaredType
var DeclaredType int

// Variable: ImpliedType
var ImpliedType = 0

// Variable: PointerType
var PointerType *float64

// Variable: ArrayType
// Array comes before the type.
var ArrayType [32]byte

// Variable: PointersAndArray1
var PointersAndArray1 [2][5]*float32

// Variable: PointersAndArray2
var PointersAndArray2 * [2] [5] float32

// Variable: SliceType
var SliceType []int

// Variable: MapType
var MapType map[string]int

// Variable: ParameterizedType1
var ParameterizedType1 OtherType [ int ]

// Variable: ParameterizedType2
var ParameterizedType2 Package.OtherType [int, [2]byte, Package.NestedType[float64, float64], map[int]string]


// Group: Channels
// ______________________________________________

// Variable: ChannelType
var ChannelType chan int

// Variable: SendOnlyChannelType
var SendOnlyChannelType chan<- int

// Variable: ReceiveOnlyChannelType
var ReceiveOnlyChannelType <-chan int


// Group: Misc
// ______________________________________________

// Variable: ShortDeclaration
// Untyped variables declared with := instead of = don't need the "var" keyword.
ShortDeclaration := 0.0


// Group: Currently Unsupported Features
// ______________________________________________

// Variables: Shared Types
// All three are float64
var A, B, C float64

// Variables: Shared Types with Values
var A, B float32 = -1, -2

// Variables: Paretheses
var (
	A int
	B float64
	)