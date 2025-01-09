
// Module: PortsAndParameterPortsA
module PortsAndParameterPortsA #(
	logic paramA,
	reg paramB = 2,
	int paramC[2] = '{'0,'0})
(	logic portA,
	reg unsigned portB = 3,
	int unsigned [7:0] portC);
endmodule

// Module: PortsAndParameterPortsB
module PortsAndParameterPortsB #(
	parameter logic paramA,
	parameter reg paramB = 2,
	localparam int paramC[2] = '{'0,'0})
(	input logic portA,
	inout reg unsigned portB = 3,
	output int unsigned [7:0] portC);
endmodule

// Module: PortsAndParameterPortsC
module PortsAndParameterPortsC #(
	parameter logic paramA,
	reg paramB = 2,
	localparam int paramC[2] = '{'0,'0})
(	input supply0 portA,
	supply1 logic portB,
	inout tri reg unsigned portC = 3,
	output triand int unsigned [7:0] portD);
endmodule

// Module: PortsAndParameterPortsD
module PortsAndParameterPortsD #(
	parameter logic paramA,
	reg paramB = 2,
	int paramC[2] = '{'0,'0})
(	(* x *) input supply0 portA,
	(* y=2 *) tri reg unsigned portB = 3,
	output triand int unsigned [7:0] portC);
endmodule

