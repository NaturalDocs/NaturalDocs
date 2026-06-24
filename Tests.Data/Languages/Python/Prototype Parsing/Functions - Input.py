
# Group: Basics
# _______________________________________________

# Function: EmptyParens
def EmptyParens ():

# Function: Params
def Params (a, b: int, c = 12, d: str = "string"):

# Function: ReturnValuesA
def ReturnValuesA () -> int:

# Function: ReturnValuesB
# Not specifying a return value defaults to Any and not None.
def ReturnValuesB () -> Any:

# Function: ReturnValuesC
# Not specifying a return value defaults to Any and not None.
def ReturnValuesC () -> None:

# Function: Async
async def Async ():

# Function: ŬnicodeÏdentifiers
def ŬnicodeÏdentifiers (ŬnicodeÅrg):

# Function: Decorators
@DecoratorA
@DecoratorB ()
@DecoratorC (12, "string")
@DecoratorD (arg1 = 12, arg2 = "string")
def Decorators (a, b: int, c: str = "string"):


# Group: Stars and Slashes
# _______________________________________________

# Function: VariablePositionalArgsA
def VariablePositionalArgsA (a, b: int, *c):

# Function: VariablePositionalArgsB
def VariablePositionalArgsB (a, b: int, *c: int):

# Function: VariablePositionalArgsC
def VariablePositionalArgsC[Ts] (a, b: int, *c: *Ts):

# Function: VariableKeywordArgsA
def VariableKeywordArgsA (a, b: int, **c):

# Function: VariableKeywordArgsB
def VariableKeywordArgsB (a, b: int, **c: int):

# Function: PositionalArgsDividerA
# A "/" parameter can appear on its own signifying arguments before it are positional only.
def PositionalArgsDividerA (a, b, /, c, d):

# Function: PositionalArgsDividerB
# A "/" parameter can appear on its own signifying arguments before it are positional only.
def PositionalArgsDividerB (a: int, b: int = 12, /, c: str, d: str = "string"):

# Function: PositionalArgsDividerC
# It can appear at the end to signify they are all positional only.
def PositionalArgsDividerC (a, b, c, /):

# Function: PositionalArgsDividerD
# It can appear at the end to signify they are all positional only.
def PositionalArgsDividerD (a: int, b: int = 12, c: str = "string", /):

# Function: KeywordArgsDividerA
# A "*" can appear on its own signifying arguments after it are keyword only.
def KeywordArgsDividerA (a, b, *, c, d):

# Function: KeywordArgsDividerB
# A "*" can appear on its own signifying arguments after it are keyword only.
def KeywordArgsDividerB (a: int, b: int = 12, *, c: str, d: str = "string"):

# Function: KeywordArgsDividerC
def KeywordArgsDividerC (*, a, b, c):

# Function: KeywordArgsDividerD
def KeywordArgsDividerD (*, a: int, b: int = 12, c: str = "string"):

# Function: MultipleDividersA
def MultipleDividersA (a, b, /, c, d, *, e, f):

# Function: MultipleDividersB
def MultipleDividersB (a: int, b: int = 12, /, c: str, d: str = "string", *, e: float, f: float = 0.0):


# Group: Parameterized Functions
# _______________________________________________

# Function: Parameterized
def Parameterized[X, Y] (a: X, b: Y):

# Function: ParameterizedWithConstraints
def ParameterizedWithConstraints[X: int, Y: (int, bytes)] (a: X, b: Y):

# Function: ParameterizedWithDefaults
def ParameterizedWithDefaults[X = int, Y: (int, bytes) = int] (a: X, b: Y):

# Function: ParameterizedWithTuples
# Tuples can have defaults but not constraints.
def ParameterizedWithTuples[*X, *Y = (int, bytes)] (a: X, b: Y):

# Function: ParameterizedWithCallable
# Callables can have defaults but not constraints.
def ParameterizedWithCallable[**X, **Y = (str, bytearray)] (a: Callable[X, int], b: Callable[Y, int]):

# Function: ParameterizedWithUnion
#
# Has a slightly different meaning than using tuples as constraints but still valid syntax.
#
# Here A can be an int and B can be bytes because the type for X is "int or bytes", whereas with a tuple
# constraint they must both be the same type because the type for X must be either "int" or "bytes", not
# "int or bytes".
#
def ParameterizedWithUnion[X: int | bytes] (a: X, b: X):


# Group: Type Expressions
# _______________________________________________

# Function: OptionalTypes
def OptionalTypes (a: int | None, b: Optional[int]):

# Function: MultipleTypes
def MultipleTypes (a: str | bytearray, b: Union[str, bytearray]) -> str | bytearray:

# Function: ParameterizedTypes
def ParameterizedTypes (a: list[int], b: set[int], c: dict[str, float], d: tuple[int, str, float],
								  e: tuple[int, ...], f: Callable[[int, int], int], g: Callable[..., int]):

# Function: MultipleParameterizedTypes
def MultipleParameterizedTypes (a: list[str | bytearray], b: list[Union[str, bytearray]]):

# Function: ParenthesesTypes
# This is allowed since any expression is allowed.
def ParenthesesTypes (a: (str | bytearray)) -> (str | bytearray):

# Function: Literals
def Literals (a: Literal["GET" | "POST"]):
