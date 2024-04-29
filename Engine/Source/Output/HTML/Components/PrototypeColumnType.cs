/*
 * Enum: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeColumnType
 * ____________________________________________________________________________
 *
 * A prototype's parameter column type.  Note that the prototype CSS classes are directly mapped to these
 * names.
 *
 *		OpeningDecorator - For Tcl prototypes, a separate column for opening parameter decorators, such as { in
 *									 {a 12}.
 *		ModifierQualifier - For C-style prototypes, a separate column for modifiers and qualifiers.  For Pascal-style
 *								  prototypes, any modifiers that appear before the name.
 *		Type - The parameter type.  For C-style prototypes this will only be the last word.  For Pascal-style
 *				  prototypes this will be the entire symbol.
 *		TypeNameSeparator - For Pascal-style prototypes, the symbol separating the name from the type.
 *		Symbols - Symbols between names and types that should be formatted in a separate column, such as *
 *					   and &.
 *		Name - The parameter name.
 *		DefaultValueSeparator - If present, the symbol for assigning a default value like = or :=.
 *		DefaultValue - The default value.
 *		PropertyValueSeparator - If present, the symbol for assigning a value to a property like = or :.
 *		PropertyValue - The property value, such as could appear in Java annotations.
 *		ClosingDecorator - For Tcl prototypes, a separate column for opening parameter decorators, such as } in
 *									{a 12}.
 *
 *
 *	SystemVerilog-Specific:
 *
 *		SystemVerilog has its own prototype formatting and thus its own token types:
 *
 *		ParameterKeywords - The keyword defining a parameter type, like "localparam" or "parameter", and/or the
 *										port direction keyword, like "input" or "inout".
 *		Signed - The data type signing, like "signed" or "unsigned".
 *		TypeBody - The body of a type defined inline, such as an enum.
 *		TypeDimension - The dimensions after the type name.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public enum PrototypeColumnType : byte
		{
		OpeningDecorator,
		ModifierQualifier, Type, TypeNameSeparator,
		Symbols, Name,
		DefaultValueSeparator, DefaultValue,
		PropertyValueSeparator, PropertyValue,
		ClosingDecorator,

		ParameterKeywords, Signed, TypeBody, TypeDimension
		}
	}
