all: UVtools clean
UVtools: UVtools.o
		gcc -o UVtools UVtools.o
UVtools.o: UVtools.c
		gcc -o UVtools.o -c UVtools.c -W -Wall -ansi -pedantic
clean:
		rm -rf *.o