#include <stdio.h>
#include <Hello.h>
#include <World.h>

// Prints "Hello, World!" by getting the Hello from one
// library and the World! from another. These two libraries
// both use the Utility library...
int main(int argc, char** argv)
{
	printf("%s%s", Hello::getText().c_str(), World::getText().c_str());
	return 0;
}