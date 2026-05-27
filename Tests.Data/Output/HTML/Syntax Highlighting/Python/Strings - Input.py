
# Topic: Single Quoted Strings
# _____________________________________________
#
# --- Code
# a 'x' a
# b '\'' b
# c '\"' c
# d '\\' d
# ---

# Topic: Double Quoted Strings
# _____________________________________________
#
# --- Code
# a "x" a
# b "\'" b
# c "\"" c
# d "\\" d
# ---


# Topic: Tripled Quoted Strings
# _____________________________________________
#
# --- Code
# a '''x 'x' x \'''x''\' x "x" x """x""" x''' a
# b """x "x" x \"""x""\" x 'x' x '''x''' x""" b
# ---


# Topic: Multiline Strings
#_____________________________________________
#
# --- Code
# a "x \
# x" b
#
# c '''x
# x''' d
#---


# Topic: String Prefixes
# _____________________________________________
#
# Raw string literals don't treat backslashes as escape characters, except sort of for quotes.  \' and \" still
# escape quotes, but the backslash character is included in the resulting string as well, so a raw string
# cannot end with a backslash character.  So effectively backslashes are still the same for our purposes
# since we only care about detecting the end of the string.
#
# --- Code
# a r"x \" x" b R'x \' x' c R'''x''' d r"""x""" e
# ---
#
# Bytes, formatted, and template strings can be combined with raw strings.
#
# --- Code
# a b'x' b RB"x" c br'''x''' d B"""x""" e
# a f'x' b RF"x" c fr'''x''' d F"""x""" e
# a t'x' b RT"x" c tr'''x''' d T"""x""" e
# ---
#
# Unicode strings are included for backwards compatibility.
#
# --- Code
# a u'x' b U'x' c
# ---


# Topic: Interpolated Strings
# _____________________________________________
#
# Formatted and template strings support interpolation.  The differences between them aren't relevant for
# syntax highlighting.
#
# --- Code
# a f"x {x} x" a
# b t'x { 'x' } x' b
# c F"""x { (x + 5) * len(T"""x""")} x""" c
# ---
#
# Literal braces are included by doubling them.
#
# --- Code
# a f"x {x} x {{x}} x" a
# ---
