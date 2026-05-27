
# Topic: Integers
#
# --- Code
# 12
# -12
# 123456789
# -123456789
# ---

# Topic: Floating Point
#
# --- Code
# 12e1
# -12E-1
# 12.34
# -12.34
# 12.34e+5
# -12.34e-5
# .012
# -.012
# .012E+5
# -.012e10
# 0.0123
# -0.0123
# 0.0123e+5
# -0.0123E-5
# ---
#
# Ending with just a dot is allowed.
#
# --- Code
# 1.
# 123.e5
# ---

# Topic: Hex, Binary, and Octal
#
# --- Code
# 0x01230ABC
# 0Xabc12345
# 0b00100110
# 0B0110100110011100
# 0o1234
# 0O567
# ---

# Topic: Digit Separators
#
# --- Code
# 1_234_567
# 0xabc1_2345
# 0b0110_1001_1001_1100
# 1.234_567e1_2
# ---

# Topic: Imaginary Numbers
#
# Imaginary numbers are represented with a j (not an i) after the constant.
#
# --- Code
# 1.23j
# .01e-12j
# 123j
# 12.j
# ---

# Topic: Traps
#
# The fact that the hex value ends in E shouldn't make it think the following token is part of
# the number as an exponent.
#
# --- Code
# 0x000E+a
# 0x000e-1
# ---
#
# The minus sign should be part of the number for -1 but not for x-1.
#
# --- Code
# -1
# x-1
# x -1
# 1-1
# 1 -1
# (-1)
# 1 + -1
# ---
#
# Digit separators can't start a number.
#
# --- Code
# _12
# ---
#
# Numbers appearing in other contexts shouldn't be mistaken for constants.
#
# --- Code
# a1_2_3 = "45"
# ---
