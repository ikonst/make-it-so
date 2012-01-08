#include <Hello.h>
#include <World.h>
#include <stdio.h>

int main(int argc, char** argv)
{
	std::string message = Hello::getText() + ", " + World::getText();
	printf(message.c_str());
	return 0;

}