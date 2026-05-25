
// Group: Typeless Variables
// ______________________________________________

// Variable: Typeless
Typeless = 12;

// Variable: TypelessVar
var TypelessVar;

// Variable: TypelessVarWithValue
var TypelessVarWithValue = 12;

// Variable: TypelessLet
let TypelessLet;

// Variable: TypelessLetWithValue
let TypelessLetWithValue = 12;


// Group: Typed Variables
// ______________________________________________

// Variable: Typed
Typed: number = 12;

// Variable: TypedVar
var TypedVar: string;

// Variable: TypedVarWithValue
var TypedVarWithValue: boolean = true;

// Variable: TypedLet
let TypedLet: string;

// Variable: TypedLetWithValue
let TypedLetWithValue: number = 12;


// Group: Special Types
// ______________________________________________

// Variable: ArrayType
var ArrayType: number[] = [1, 2, 3];

// Variable: UserDefinedType
let UserDefinedType: MyClass;

// Variable: TemplatedType
TemplatedType: MyTemplate<number, string>;

// Variable: OptionalVariable
// May appear in interfaces
OptionalVariable?: number;

// Variable: AllowableValues
var AllowableValues: "x" | "y" | "z";


// Group: Modifiers
// ______________________________________________

// Variable: ReadOnlyTyped
readonly ReadOnlyTyped: number;

// Variable: ReadOnlyUntyped
readonly ReadOnlyUntyped;

// Variable: DecoratedVariable
@ParameterlessDecorator
@ParameterDecorator(value)
var DecoratedVariable;
