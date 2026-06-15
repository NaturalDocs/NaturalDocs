
# Group: Standard Decorators
# _______________________________________________

# Function: SimpleDecorators
@DecoratorA
@DecoratorB()
@DecoratorC ()
def SimpleDecorators ():

# Function: DecoratorsWithValues
# If the parameters aren't named, format them on a single line.
@DecoratorA(12, "string")
def DecoratorsWithValues ():

# Function: DecoratorsWithNamedArgs
# If the parameters are named, format them like function parameters.
@DecoratorA(FirstArg = 12, SecondArg = "string")
def DecoratorsWithNamedArgs ():

# Function: DecoratorsWithMixedArgs
# If a named parameter follows an unnamed one, still format them on a single line.
@DecoratorA(12, SecondArg = "string")
def DecoratorsWithMixedArgs ():


# Group: Extended Decorators
# _______________________________________________
#
# Decorators were extended in <PEP 614: https://peps.python.org/pep-0614/> to allow any expression.
# We'll format standard decorators and just highlight any other text that appears after it.
#

# Function: ExtendedDecoratorsA
@LookupDecoratorA["string"]
@LookupDecoratorB()[12]
def ExtendedDecoratorsA ():

# Function: ExtendedDecoratorsB
@Chained.Decorator("string").More(FirstArg = 12).More
def ExtendedDecoratorsB ():

# Function: ExtendedDecoratorsC
@(WalrusOperator := "string")
def ExtendedDecoratorsC ():


# Group: Misc
# _______________________________________________

# Function: AllDecorators
@DecoratorA
@DecoratorB()
@DecoratorCWithArgs(12, "string")
@DecoratorDWithNamedArgs(FirstArg = 12, SecondArg = "string")
@DecoratorEWithMixedArgs(12, "string")
@LookupDecoratorA["string"]
@LookupDecoratorB()[12]
@Chained.Decorator("string").More(FirstArg = 12).More
@(WalrusOperator := "string")
def AllDecorators ():

# Function: AllDecoratorsWithSpaces
@ DecoratorA
@ DecoratorB ( )
@ DecoratorCWithArgs ( 12, "string" )
@ DecoratorDWithNamedArgs ( FirstArg = 12, SecondArg = "string" )
@ DecoratorEWithMixedArgs ( 12, "string" )
@ LookupDecoratorA [ "string" ]
@ LookupDecoratorB () [ 12 ]
@ Chained.Decorator ( "string" ).More ( FirstArg = 12 ).More
@ (WalrusOperator := "string")
def AllDecoratorsWithSpaces ():
