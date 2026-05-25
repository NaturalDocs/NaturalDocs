
// Type: ObjectType
type ObjectType = {
	a: number;
	b: number;
	}

// Type: UnionType
type UnionType = string | number;

// Type: IntersectionType
type IntersectionType = NamedTypeA & NamedTypeB;

// Type: TupleType
type TupleType = [string, number];

// Type: ConditionalType
type ConditionalType = X extends Y ? A : B;
