
// Group: Parameter Types
// ______________________________________________

// Function: ParameterTypes
function ParameterTypes(a: string, b: Date, c: boolean = false)
	{  }

// Function: PartialParameterTypes
// Types are not inherited, so "a" is not a string.
function PartialParameterTypes(a, b: string, c: any, d: boolean = false, e = 12)
	{  }

// Function: ArrayParameter
function ArrayParameter (a: number[])
	{  }

// Function: UserDefinedTypeParameter
function UserDefinedTypeParameter (a: UserDefinedType)
	{  }

// Function: ParameterizedTypeParameter
function ParameterizedTypeParameter (a: ParameterizedType<string>, b: ParameterizedType<UserType, number>)
	{  }

// Function: InlineObjectParameter
function InlineObjectParameter(a: { x: number; y: number })
	{  }

// Function: TupleParameter
function TupleParameter (a: [number, string])
	{  }

// Function: OptionalParameters
function OptionalParameters (a: number, b?: number)
	{  }

// Function: UnionParameters
function UnionParameters (a: number | string, b: string | string[] | undefined)
	{  }

// Function: AllowableValueParameters
function AllowableValueParameters (a: "x" | "y" | "z", b: 1 | 0 | -1)
	{  }

// Function: RestParameter
function RestParameter (a: number, ...b: number[])
	{  }

// Function: DecoratedParameter
function DecoratedParameter (@Decorator1 @Decorator2 a, @ParameterDecorator(value) b: number)
	{  }



// Group: Return Values
// ______________________________________________

// Function: ReturnNumber
function ReturnNumber(): number
	{  }

// Function: ReturnArray
function ReturnArray (): string[]
	{  }

// Function: ReturnVoid
function ReturnVoid (): void
	{  }

// Function: ReturnPromise
function ReturnPromise(): Promise<number>
	{  }

// Function: AllowableReturnValues
function AllowableReturnValues(): 1 | 0 | -1
	{  }

// Function: TypePredicateReturnValue
function TypePredicateReturnValue (a): a is UserDefinedType
	{  }


// Group: Generic Functions
// ______________________________________________

// Function: SingleGenericType
function SingleGenericType<T>(a: Type[]): Type | undefined
	{  }

// Function: TwoGenericTypes
function TwoGenericTypes<X,Y>(a: X[]): Y[]
	{  }

// Function: SimpleConstraint
function SimpleConstraint<T extends any[]>()
	{  }

// Function: ObjectConstraint
function ObjectConstraint<T extends { x: number }>()
	{  }

// Function: ConstraintWithDefault
function ConstraintWithDefault<T extends MyType[] = MyOtherType[]>()
	{  }

// Function: ConstraintsWithVariance
function ConstraintsWithVariance<in A, out B, in out C>()
	{  }

// Function: KeyOfTypeOperator
function KeyOfTypeOperator<A, B extends keyof A>()
	{  }


// Group: Modifiers
// ______________________________________________

// Function: PublicFunction
public function PublicFunction ()
	{  }

// Function: ProtectedFunction
protected function ProtectedFunction()
	{  }

// Function: StaticFunction
static function StaticFunction()
	{  }

// Function: AbstractFunction
abstract function AbstractFunction();

// Function: DecoratedFunction
@ParameterlessDecorator
@ParameterDecorator(value)
function DecoratedFunction()
	{  }