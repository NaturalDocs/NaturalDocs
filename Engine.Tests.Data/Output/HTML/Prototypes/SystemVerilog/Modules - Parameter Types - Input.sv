
// Group: Data Types

// Module: IntegerVectorTypes
module IntegerVectorTypes (logic a, reg [7:0] b, bit unsigned [12:0] c, bit signed [10:0][] d);

// Module: IntegerAtomTypes
module IntegerAtomTypes (byte a, shortint signed b, int c, longint unsigned d, integer e, time f);

// Module: NonIntegerTypes
module NonIntegerTypes (shortreal a, real b, realtime c);

// Module: StructTypes
module StructTypes (
	struct { int aa } a,
	struct tagged { int bb } b,
	struct tagged packed { int cc } [7:0] c,
	struct tagged packed unsigned { int dd } d [7:0],
	struct packed { int ee } e,
	union { int ff } f,
	union tagged { int gg } g,
	union tagged packed { int hh } [12:0] h,
	union tagged packed signed { int ii } i [12:0],
	union packed { int jj } j
	);

// Module: EnumTypes
module EnumTypes (
	enum { a1, a2[1], a3[2:3], a4 = 3, a5[2] = 1 } a,
	enum byte { b1 } b,
	enum int signed { c1 = 0 }[4:0] c, 
	enum bit unsigned [7:0] { d1 }[][] d
	);

// Module: VirtualInterfaceTypes
module VirtualInterfaceTypes (
	virtual VIT a,
	virtual VIT #(35) b,
	virtual VIT #(16).phy c,
	virtual interface VIT d,
	virtual interface VIT #(35) e,
	virtual interface VIT #(16).phy f
	);

// Module: TypeIdentifiers
module TypeIdentifiers (
	TypeA a,
	ClassB::TypeB b,
	PackageC::ClassC::TypeC c,
	$unit::TypeD d,
	$unit::ClassE::TypeE e,
	ClassF #(x, 12)::TypeF f,
	ClassG#($)::TypeG g,
	$unit::ClassH#(12)::TypeH h[12:0],
	$unit::ClassI #(12)::TypeI i [12:0]
	);

// Module: OtherTypes
module OtherTypes (string a, chandle b, event c);

// Module: TypeReferences
module TypeReferences (
	type(string) a, 
	type (string) b,
	type ($unit::ClassC #(12)::TypeC) c
	);

