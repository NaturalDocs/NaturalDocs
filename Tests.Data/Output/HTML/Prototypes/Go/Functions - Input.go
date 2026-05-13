
// Function: NoReturnValue
func NoReturnValue (a int)
	{  }

// Function: SimpleReturnValue
func SimpleReturnValue (a int) int
	{  }

// Function: Parameterized
func Parameterized[A comparable, B int32, C ~int|~float64] ()
	{  }

// Function: Receiver
// Extension functions/"this" parameters are called receivers in Go.
func (r *ReceiverType) Receiver() float64
	{  }

// Function: ParameterizedReceiver
func (r ReceiverType[T]) ParameterizedReceiver() T
	{  }
	
// Function: MultipleReturnValuesA
func MultipleReturnValuesA () (bool, int)
	{  }
	
// Function: MultipleReturnValuesB
func MultipleReturnValuesB () (success bool, value int)
	{  }

// Function: ParametersAndMultipleReturnValuesA
func ParametersAndMultipleReturnValuesA (a string, b int) (bool, int)
	{  }
	
// Function: ParametersAndMultipleReturnValuesB
func ParametersAndMultipleReturnValuesB (a string, b int) (success bool, value int)
	{  }

// Function: UnnamedParameters
func UnnamedParameters (float64, *[12]byte, OtherType[int], map[int]string)
	{  }
	
	
// Group: Partially Supported Features
// ______________________________________________

// Function: InterfaceConstraintsA
// We support constraints using interface{} but currently don't check inside their braces for types.
func InterfaceConstraintsA[A interface{}, B interface{any}, C interface{~int|[]OtherType}] ()
	{  }

// Function: InterfaceConstraintsB
// We support constraints using interface{} but currently don't check inside their braces for types.
func InterfaceConstraintsB[T interface{ FuncA(); FuncB(b int) bool; float32 | float64 }] ()
	{  }

// Function: AmbiguousUnnamedParameters
// Unnamed parameters where the types are identifiers that can be interpreted as names instead of types
// will be interpreted as names since there isn't an easy way to tell the difference and them being names
// is the much more common case.
func AmbiguousUnnamedParameters (TypeName1, TypeName2)
	{  }
	