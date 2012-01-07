#include "Comma.h"
#include "Hello.h"
extern "C"
{
	#include "World.h"
}
#include <stdio.h>

int main(int argc, char** argv)
{
	std::string message = Hello::getText() + Comma::getText() + getWorld();
	printf(message.c_str());
	return 0;
}