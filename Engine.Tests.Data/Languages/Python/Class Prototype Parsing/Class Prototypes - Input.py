
# Class: Python_Plain
class Python_Plain:

# Class: Python_Empty_Parens
class Python_Empty_Parens ():

# Class: Python_Inheritance
class Python_Inheritance (Python_Base):

# Class: Python_Module_Inheritance
class Python_Module_Inheritance (module.Python_Base):

# Class: Python_Multiple_Inheritance
class Python_Multiple_Inheritance (Python_BaseA, Python_BaseB, module.Python_BaseC):

# Class: Ŭnicode_Pŷthon
class Ŭnicode_Pŷthon (Ŭnicode_Pŷthon_Base):

# Class: Python_Decorators
@f1(arg)
@f2
class Python_Decorators:

# Class: Python_Metaclass
class Python_Metaclass (metaclass=Python_Metaclass):

# Class: Everything
@f1(arg)
@f2
class Everything (metaclass=Python_Metaclass, Ŭnicode_Pŷthon_Base, module.Python_Module_Base):
