
# Function: MultipleDecorators
# Multiple decorators should *not* be aligned because the part before the parameters could be wildly
# different lengths.
@f1(arg1 = 12, arg2 = "string")
@f123(arg123 = 6)
def MultipleDecorators ():

# Function: DecoratorsAndParameters
# Decorator properties and function parameters should *not* be aligned because they are different
# types.
@f1(arg1 = 12, arg2 = "string")
def DecoratorsAndParameters (a: int, b: int = 12):
