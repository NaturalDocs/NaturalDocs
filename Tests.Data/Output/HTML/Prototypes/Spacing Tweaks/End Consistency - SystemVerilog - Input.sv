
// Title: End Consistency
// The closing *) should align for both sections.

// Module: EndConsistency1
(* optimize_power = 0,
   mode = "cla" *)
(* no_value *)
module EndConsistency1 ();
endmodule

// Module: EndConsistency2
(* mode = "cla",
    optimize_power = 0 *)
(* no_value *)
module EndConsistency2 ();
endmodule

// Module: EndConsistency3
(* optimize_power = 0,
   mode = "cla" *)
(* something_else = "abc" *)
module EndConsistency3 ();
endmodule

// Module: EndConsistency4
(* mode = "cla",
    optimize_power = 0 *)
(* something_else = "abc" *)
module EndConsistency4 ();
endmodule

// Module: EndConsistency5
(* optimize_power = 0,
   mode = "cla" *)
(* something_else = "abcd" *)
module EndConsistency5 ();
endmodule

// Module: EndConsistency6
(* mode = "cla",
    optimize_power = 0 *)
(* something_else = "abcd" *)
module EndConsistency6 ();
endmodule
