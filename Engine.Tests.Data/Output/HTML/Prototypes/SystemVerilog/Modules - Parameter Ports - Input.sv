

// Group: Empty Parentheses
// ___________________________________________________

// Module: EmptyA
module EmptyA #() ();
endmodule

// Module: EmptyB
module EmptyB #( ) ( );
endmodule



// Group: Leading Assignments
// They can precede typed declarations
// ___________________________________________________


// Module: LeadingAssignmentsA
module LeadingAssignmentsA #(x = 12) ();
endmodule

// Module: LeadingAssignmentsB
module LeadingAssignmentsB #(x[2] = '{'0, '0}) ();
endmodule

// Module: LeadingAssignmentsC
module LeadingAssignmentsC #(x[0:1][2] = '{'{'0,'0},'{'1,'1}}) ();
endmodule

// Module: LeadingAssignmentsD
module LeadingAssignmentsD #(a = 12, b[2] = '{'0,'0}, c[2][0:1] = '{'{'0,'0},'{'1,'1}}) ();
endmodule



// Group: Typed Declarations
// ___________________________________________________


// Module: IntegerVectorTypes
module IntegerVectorTypes #(
	bit a = '0,
	logic [7:0] b = 12,
	reg unsigned c = '1,
	bit signed [7:0] d = 'z)
	();
endmodule

// Module: IntegerAtomTypes
module IntegerAtomTypes #(
	byte a = 1,
	shortint unsigned b = 2,
	int signed c = 3,
	longint d = 4,
	integer unsigned e = 5,
	time f = 1ms)
	();
endmodule

// Module: NonIntegerTypes
module NonIntegerTypes #(
	shortreal a = 1.0,
	real b = 2.0,
	realtime c = 3.0s)
	();
endmodule

// Module: StringType
module StringType #(
	string a = "aaa"	)
	();
endmodule

// Module: TypeReferences
module TypeReferences #(
	bit [7:0] a = '1,
	int signed b = 12,
	realtime c = 1.0ms,
	string d = "ddd",
	type(a) aa = '0,
	type (b) bb = 9,
	type( c ) cc = 0.5s,
	type ( d ) dd = "x")
	();
endmodule



// Group: Valueless Declarations
// This is explicitly mentioned as valid in the specification but most compilers don't support it
// ___________________________________________________


// Module: ValuelessDeclarationsA
module ValuelessDeclarationsA #(x) ();
endmodule

// Module: ValuelessDeclarationsB
module ValuelessDeclarationsB #(x[2]) ();
endmodule

// Module: ValuelessDeclarationsC
module ValuelessDeclarationsC #(x[0:1][2]) ();
endmodule

// Module: ValuelessDeclarationsD
module ValuelessDeclarationsD #(a, b[2], c[2][0:1]) ();
endmodule

// Module: ValuelessIntegerVectorTypes
module ValuelessIntegerVectorTypes #(
	bit a,
	logic [7:0] b,
	reg unsigned c,
	reg signed [7:0] d)
	();
endmodule

// Module: ValuelessIntegerAtomTypes
module ValuelessIntegerAtomTypes #(
	byte a,
	shortint unsigned b,
	int signed c,
	longint d,
	integer unsigned e,
	time f)
	();
endmodule

// Module: ValuelessNonIntegerTypes
module ValuelessNonIntegerTypes #(
	shortreal a,
	real b,
	realtime c)
	();
endmodule

// Module: ValuelessStringType
module ValuelessStringType #(
	string a)
	();
endmodule

// Module: ValuelessTypeReferences
module ValuelessTypeReferences #(
	bit [7:0] a,
	int signed b,
	realtime c,
	string d,
	type(a) aa,
	type (b) bb,
	type( c ) cc,
	type ( d ) dd)
	();
endmodule



// Group: Implied Types
// ___________________________________________________


// Module: ImpliedTypesA
module ImpliedTypesA #(
	bit a = 1,
	aa = 1,
	logic [7:0] b = 2,
	bb = 2,
	int unsigned c = 3,
	cc = 3,
	bit unsigned [3:0] d = 4,
	dd = 4)
	();
endmodule


// Module: ImpliedTypesB
module ImpliedTypesB #(
	bit a[2] = '{1,1},
	aa = 1,
	logic b = 2,
	bb[2] = '{2,2},
	reg c[0:1] = '{3,3},
	cc[3] = '{3,3,3})
	();
endmodule


// Module: ImpliedTypesC
module ImpliedTypesC #(
	bit a = 1,
	unsigned aa = 1,
	reg signed b = 2,
	unsigned bb = 2,
	bit c = 3,
	[3:0] cc = 3,
	reg [7:0] d = 4,
	[3:0] dd = 4,
	bit signed [7:0] e = 5,
	unsigned ee = 5,
	reg signed [7:0] f = 6,
	[3:0] ff = 6,
	bit signed [7:0] g = 7,
	unsigned [3:0] gg = 7)
	();
endmodule


// Module: ImpliedTypesD
module ImpliedTypesD #(
	bit a[2] = '{1,1},
	unsigned aa = 1,
	logic [3:0] b = 2,
	[7:0] bb[2] = '{2,2},
	reg signed [7:0] c = 3,
	cc[3] = '{3,3,3})
	();
endmodule


// Module: ImpliedTypesE
module ImpliedTypesE #(
	reg a = 1,
	aa = 2,
	type(a) b = 2,
	bb = 3)
	();
endmodule



// Group: Parameter Keywords
// ___________________________________________________


// Module: ParameterKeywordsA
module ParameterKeywordsA #(
	a = 1,
	parameter b = 2,
	localparam c = 3)
	();
endmodule


// Module: ParameterKeywordsB
module ParameterKeywordsB #(
	bit a = 1,
	parameter logic [7:0] b = 2,
	localparam int unsigned c = 3)
	();
endmodule


// Module: ParameterKeywordsC
module ParameterKeywordsC #(
	a = 1,
	aa = 2,
	parameter b = 3,
	bb = 4,
	localparam c = 5,
	cc = 6)
	();
endmodule


// Module: ParameterKeywordsD
module ParameterKeywordsD #(
	bit a = 1,
	aa = 2,
	parameter logic b = 3,
	bb = 4,
	localparam int unsigned c = 5,
	cc = 6,
	localparam reg signed [7:0] d = 7,
	dd = 8)
	();
endmodule


// Module: ParameterKeywordsE
module ParameterKeywordsE #(
	bit a = 1,
	int aa = 2,
	parameter logic b = 3,
	[7:0] bb = 4,
	localparam int c = 5,
	signed cc = 6)
	();
endmodule



// Group: Misc
// ___________________________________________________


// Module: LeadingAssignmentsAndTypes
module LeadingAssignmentsAndTypes #(
	a = 12,
	b[2] = '{'0, '0},
	logic [7:0] x = 12,
	real y = 2.0,
	string z = "aaa" )
	();
endmodule

// Module: PackedAndUnpackedArrays
module PackedAndUnpackedArrays #(
	bit [7:0] a[2] = '{'0, '0},
	bit [3:0] b[0:1] = '{'0, '0})
	();
endmodule

// Module: TypeReferencesAndUnpackedDimensions
module TypeReferencesAndUnpackedDimensions #(
	logic a[2] = '{'0,'0},
	reg b = 2,
	type(a) aa = 3,
	type(b) bb[2] = '{'0,'0})
	();
endmodule

// Module: TypeAssignments
module TypeAssignments #(
	type typeA = int,
	parameter type typeB = reg unsigned,
	localparam type typeC = bit signed [7:0],
	typeA paramX = 4,
	parameter typeB paramY = 5,
	localparam typeC paramZ = 6)
	();
endmodule

// Module: Macros
module Macros #(
	`MacroA = 1,
	`MacroB(12) = 2,
	parameter `MacroC(12,16) = 3,
	parameter logic d [`MacroD:0] = 4,
	logic e = `MacroE)
	();
endmodule
