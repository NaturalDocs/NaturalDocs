
// Module: ModuleKeyword
module ModuleKeyword ();
endmodule

// Module: MacroModuleKeyword
macromodule MacroModuleKeyword ();
endmodule

// Module: StaticLifetime
module static StaticLifetime ();
endmodule

// Module: AutomaticLifetime
module automatic AutomaticLifetime ();
endmodule

// Module: Complex_Identifier$
module Complex_Identifier$ ();
endmodule

// Module: NamedEnd
module NamedEnd ();
endmodule: NamedEnd

// Module: NamedEnd_Complex_Identifier$
module NamedEnd_Complex_Identifier$ ();
endmodule: NamedEnd_Complex_Identifier$


// Group: Port Lists
// ______________________________________________

// Module: PortListOnlyA
module PortListOnlyA ();
endmodule

// Module: PortListOnlyB
module PortListOnlyB (logic portA);
endmodule

// Module: ParameterPortListOnlyA
module ParameterPortListOnlyA #();
endmodule

// Module: ParameterPortListOnlyB
module ParameterPortListOnlyB #(parameter paramA = 1);
endmodule

// Module: PortAndParameterPortListsA
module PortAndParameterPortListsA #() ();
endmodule

// Module: PortAndParameterPortListsB
module PortAndParameterPortListsB #(parameter paramA = 1) (logic portA);
endmodule

// Module: PortAndParameterPortListsC
module PortAndParameterPortListsC #() (logic portA);
endmodule

// Module: PortAndParameterPortListsD
module PortAndParameterPortListsD #(parameter paramA = 1) ();
endmodule

// Module: NoPortLists
module NoPortLists;
endmodule
