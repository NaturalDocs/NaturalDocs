
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
module LeadingAssignmentsC #(x[2][2] = '{'{'0,'0},'{'1,'1}}) ();
endmodule

// Module: LeadingAssignmentsD
module LeadingAssignmentsD #(a = 12, b[2] = '{'0,'0}, c[2][2] = '{'{'0,'0},'{'1,'1}}) ();
endmodule



// Group: Leading Assignments (unsupported)
// This is explicitly mentioned as valid in the specification but most compilers don't support it
// ___________________________________________________


// Module: UnsupportedLeadingAssignmentsA
module UnsupportedLeadingAssignmentsA #(x) ();
endmodule

// Module: UnsupportedLeadingAssignmentsB
module UnsupportedLeadingAssignmentsB #(x[2]) ();
endmodule

// Module: UnsupportedLeadingAssignmentsC
module UnsupportedLeadingAssignmentsC #(x[2][2]) ();
endmodule

// Module: UnsupportedLeadingAssignmentsD
module UnsupportedLeadingAssignmentsD #(a, b[2], c[2][2]) ();
endmodule



// Group: Parameter Port Declarations
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
	type(b) bb = 9,
	type(c) cc = 0.5s,
	type(d) dd = "x")
	();
endmodule



// Group: Parameter Port Declarations (unsupported)
// This is explicitly mentioned as valid in the specification but most compilers don't support it
// ___________________________________________________


// Module: UnsupportedIntegerVectorTypes
module UnsupportedIntegerVectorTypes #(
	bit a,
	logic [7:0] b,
	reg unsigned c,
	reg signed [7:0] d)
	();
endmodule

// Module: UnsupportedIntegerAtomTypes
module UnsupportedIntegerAtomTypes #(
	byte a,
	shortint unsigned b,
	int signed c,
	longint d,
	integer unsigned e,
	time f)
	();
endmodule

// Module: UnsupportedNonIntegerTypes
module UnsupportedNonIntegerTypes #(
	shortreal a,
	real b,
	realtime c)
	();
endmodule

// Module: UnsupportedStringType
module UnsupportedStringType #(
	string a)
	();
endmodule

// Module: UnsupportedTypeReferences
module UnsupportedTypeReferences #(
	bit [7:0] a,
	int signed b,
	realtime c,
	string d,
	type(a) aa,
	type(b) bb,
	type(c) cc,
	type(d) dd)
	();
endmodule



// Group: Implied Types
// ___________________________________________________


// Module: ImpliedTypesA
// Note that the trailing [2] in C and E does *not* get inherited by CC and EE.
module ImpliedTypesA #(
	logic a = 1,
	aa = 1,
	logic [7:0] b = 2,
	bb = 2,
	logic c[2] = '{3,3},
	cc = 3,
	logic unsigned d = 4,
	dd = 4,
	logic unsigned [3:0] e[2] = '{5,5},
	ee = 5)
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
	bit [7:0] a[2] = '{'0, '0})
	();
endmodule
