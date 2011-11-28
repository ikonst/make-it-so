#include <Addition.h>
#include <Multiplication.h>
#include <stdio.h>


int main(int argc, char** argv)
{
	double a = Addition::add(6.0, 7.0);
	double b = Multiplication::multiply(8.0, 9.0);

	printf("6+7:%.2lf,8*9:%.2lf", a, b);

	return 0;
}