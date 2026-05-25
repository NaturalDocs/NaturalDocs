
// Interface: SimpleInterface
interface SimpleInterface {
	}

// Interface: InheritedInterface
interface InheritedInterface extends BaseInterface {
	}

// Interface: MultipleInheritedInterfaces
interface MultipleInheritedInterfaces extends BaseInterfaceA, BaseInterfaceB {
	}

// Interface: GenericInterface
interface GenericInterface<X, Y> {
	}

// Interface: GenericInterfaceWithInheritance
interface GenericInterfaceWithInheritance<T> extends BaseInterface<T> {
	}

// Interface: VarianceAnnotations
interface VarianceAnnotations<in X, out Y, in out Z> {
	}