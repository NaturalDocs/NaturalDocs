
// Module: Attributes
// Multiple consecutive attributes *should* be aligned because they are the same type and they have
// short content before the parameters.
(* optimize_power = 0 *)
(* mode = "cla" *)
(* expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module Attributes1 ();

// Module: NoValue1
// Attributes without values should also be treated as parameter sections so that the closing *) are
// still aligned.
(* mode = "cla" *)
(* no_value *)
(* optimize_power = 0 *)
module NoValue1 ();

// Module: NoValue2
(* no_value *)
(* mode = "cla" *)
module NoValue2 ();

// Module: NoValue3
(* no_value1 *)
(* no_value2 *)
(* optimize_power = 0 *)
module NoValue3 ();
