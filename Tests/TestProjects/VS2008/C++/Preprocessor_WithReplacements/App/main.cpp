#include <stdio.h>

int main(int argc, char** argv)
{
	#ifdef GCC_BUILD
		printf("Hello, ");
	#endif

	#ifdef TO_REPLACE
		printf("Richard");
	#endif

	#ifdef REPLACEMENT
		printf("Wor");
	#endif

	#ifdef NEW_DEFINITION_DEBUG
		printf("ld!");
	#endif

	#ifdef NEW_DEFINITION_RELEASE
		printf("m!");
	#endif

	return 0;
}