
// Enums should appear under the Types heading

class EnumsOnly
	{
	enum EnumA { }
	enum EnumB { }
	}

class TypesOnly
	{
	// Type: TypeA

	// Type: TypeB
	}

class EnumsAndTypes
	{
	enum EnumA { }

	// Type: TypeA

	enum EnumB { }

	// Type: TypeB
	}

class TypesAndEnums
	{
	// Type: TypeA

	enum EnumA { }

	// Type: TypeB

	enum EnumB { }
	}

class EnumsWithMembers
	{
	// Enum: EnumA
	// Value1 - Description
	// Value2 - Description

	// Type: TypeA
	}