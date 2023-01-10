
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
	enum bit unsigned [7:0] { d1 }[][] d);

// Module: OtherTypes
module OtherTypes (string a, chandle b, event c);