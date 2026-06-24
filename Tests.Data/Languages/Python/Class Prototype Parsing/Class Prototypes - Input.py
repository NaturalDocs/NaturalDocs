
# Class: Simple
class Simple:


# Group: Inheritance
# _______________________________________________

# Class: Inheritance
class Inheritance (Base):

# Class: ModuleInheritance
class ModuleInheritance (module.Base):

# Class: MultipleInheritance
class MultipleInheritance (BaseA, BaseB, module.BaseC):

# Class: Metaclass
class Metaclass (metaclass=MetaclassBase):

# Class: EmptyParens
class EmptyParens ():


# Group: Parameterized Classes
# _______________________________________________

# Class: Parameterized
class Parameterized[X, Y]:

# Class: ParameterizedWithConstraints
class ParameterizedWithConstraints[X: int, Y: (int, bytes)]:

# Class: ParameterizedWithDefaults
class ParameterizedWithDefaults[X = int, Y: (int, bytes) = int]:

# Class: ParameterizedWithTuples
# Tuples can have defaults but not constraints.
class ParameterizedWithTuples[*X, *Y = (int, bytes)]:

# Class: ParameterizedWithCallable
# Callables can have defaults but not constraints.
class ParameterizedWithCallable[**X, **Y = (str, bytearray)]:

# Class: ParameterizedWithUnion
class ParameterizedWithUnion[X: int | bytes] (a: X, b: X):


# Group: Misc
# _______________________________________________

# Class: ŬnicodeIdëntifiers
class ŬnicodeIdëntifiers (ŬnicodeBåse):

# Class: Decorators
@DecoratorA
@DecoratorB ()
@DecoratorC (12, "string")
@DecoratorD (arg1 = 12, arg2 = "string")
class Decorators:

# Class: AllCombined
@Decorator (arg1 = 12, arg2 = "string")
class AllCombined[X: int | bytes, Y: (int, bytes)] (BaseA, metaclass=module.BaseB):
