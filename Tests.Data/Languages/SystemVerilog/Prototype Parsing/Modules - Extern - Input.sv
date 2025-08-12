
// Module: Extern
extern module Extern ();
endmodule

// Module: ExternWithAttributes1
// Extern appears *before* the attributes if there are any present.
extern (* optimize_power=1 *) module ExternWithAttributes1 ();
endmodule

// Module: ExternWithAttributes2
// Extern appears *before* the attributes if there are any present.
extern
(* optimize_power=1 *)
module ExternWithAttributes2 ();
endmodule