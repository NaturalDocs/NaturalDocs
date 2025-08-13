
module NoPorts;
endmodule

module Ports (input logic x);
endmodule

module ParamPorts #(parameter a = 12) (input logic x);
endmodule

macromodule MacroModule #() ();
endmodule

extern module Extern ();
endmodule

extern (* optimize_power=1 *) module ExternWithAttributes ();
endmodule

module ParentModule ();

	module ChildModule ();
	endmodule

endmodule