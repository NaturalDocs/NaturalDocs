
// Enum: SimpleEnum
enum SimpleEnum { A, B, C }

// Enum: ClassEnum
class enum ClassEnum { A, B, C }

// Enum: StructEnum
struct enum StructEnum { A, B, C }

// Enum: TrailingComma
enum TrailingComma { A, B, C, }

// Enum: AttributesA
[[AttributeA, AttributeB(12)]] [[AttributeC()]] enum AttributesA { A, B, C }

// Enum: AttributesB
enum [[AttributeA, AttributeB(12)]] [[AttributeC()]] AttributesB { A, B, C }

// Enum: AttributesC
[[AttributeA, AttributeB(12)]] enum [[AttributeC()]] AttributesC { A, B, C }

// Enum: NestedName
enum Namespace::NestedName { A, B, C }

// Enum: TypedA
enum TypedA : int { A, B, C }

// Enum: TypedB
enum TypedB: unsigned long { A, B, C }

// Enum: NoValues
enum NoValues;
