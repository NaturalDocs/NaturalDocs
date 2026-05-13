	
// Function: BasicTypes
// Both b and c are strings.
func BasicTypes (a int, b, c string)
	{  }

// Function: DefaultValues
func DefaultValues (a int = 12, b = 12, c float64 = 0.1, d = 0.1)
	{  }

// Function: PointersAndArrays
// Array info appears before the base type.
func PointersAndArrays (a [32]byte, b *float64, c [1000]*float64, d *[3][5]int)
	{  }

// Function: SlicesAndMaps
func SlicesAndMaps (a []int, b map[string]int)
	{  }

// Function: Channels
func Channels (a chan int, b chan<- int, c <-chan int)
	{  }

// Function: ParameterizedTypes
func ParameterizedTypes (a OtherType[int], b Package.OtherType [int, [2]byte, Package.NestedType[float64, float64], map[int]string])
	{  }
	
// Function: VariadicParameter
// Similar to "params" in C#, can have zero or more parameters passed
func VariadicParameter (a string, b ...int)
	{  }


// Group: Partially Supported Features
// ______________________________________________

// Function: InlineStruct
// We support inline structs but currently don't check inside their braces for types.
func InlineStruct (x struct { z int })
	{  }
	
// Function: InlineInterface
// We support inline interfaces but currently don't check inside their braces for types.
func InlineInterface (x interface { FunctionName() }) 
	{  }