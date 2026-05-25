
// Class: SimpleClass
class SimpleClass {
	}

// Class: InheritedClass
class InheritedClass extends BaseClass {
	}

// Class: InheritedInterface
class InheritedInterface implements BaseInterface {
	}

// Class: MultipleInheritedClass
class MultipleInheritedClass extends BaseClassA, BaseClassB {
	}

// Class: GenericClass
class GenericClass<X, Y> {
	}

// Class: GenericClassWithInheritance
class GenericClassWithInheritance<T> extends BaseClass<T> {
	}

// Class: VarianceAnnotations
class VarianceAnnotations<in X, out Y, in out Z> {
	}

// Class: AbstractClass
abstract class AbstractClass {
	}

// Class: DecoratedClass
@ParameterlessDecorator
@ParameterDecorator(value)
class DecoratedClass {
	}