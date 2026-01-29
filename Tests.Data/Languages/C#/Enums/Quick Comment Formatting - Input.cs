
/* Enum: BasicFormatting
 *
 * Values:
 *    A - Comment description of A with *bold* and _underline_ and
 *			 email@addresses.com and <https://www.naturaldocs.org> and
 *			 <named links: https://www.naturaldocs.org>.
 */
enum BasicFormatting {
	A,
	B, /// Inline description of B with *bold* and _underline_ and
	   // email@addresses.com and <https://www.naturaldocs.org> and
	   // <named links: https://www.naturaldocs.org>.
	C /** Inline description of C with *bold* and _underline_ and
	   email@addresses.com and <https://www.naturaldocs.org> and
	   <named links: https://www.naturaldocs.org>. */
	}
