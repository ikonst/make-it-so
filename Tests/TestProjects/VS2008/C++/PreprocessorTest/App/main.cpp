#include <stdio.h>

int main(int argc, char** argv)
{
	#ifdef HELLO
		printf("Hello, ");
	#endif

	#ifdef WORLD
		printf("World!");
	#endif

	return 0;
}