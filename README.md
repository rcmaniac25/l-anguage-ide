# Project Description #
A really basic IDE for a theoretical language (called L) with 3 operations.

## Background ##
In a class I have to take we are going over an introduction to "Computation". One of the first things we touched on was a computable language. We were given a language with exactly 3 instructions:

* X <- X + 1
* X <- X - 1
* IF X != 0 GOTO Y

We did the usual student reaction and just sat there doing out own thing. The professor seemed a little annoyed and told us all to close our laptops. We did. She asked us to do a couple problems using just those instructions. We ran into issues obviously, there is no "set" operation (x = y) or any "equals" operation (IF X = 0). But we were given 2 helper pieces of information, we could introduce variables whenever we liked (something we didn't know at first so we were trying to figure out how to do some of these operations with the given variables) and macros (combinations of instructions and macros).

We started to joke about doing really complex operations with this language. I said "Maybe I can write a little IDE for it." They all got a good kick out of it and I decided to see if I could do it.

## 11 hours, 50 minutes later... ##
I give to you "L(anguage) IDE" It's an IDE that supports "L" (as I have been told it is called). It supports editing (obviously), saving, loading, and debugging. It supports all 3 operations (as well as a 4th that I found was necessery) and macros.

The project is named Language Debugger because that was what it was originally intended to be used for, but it's a Language IDE for L so I call it... L(anguage) IDE.

The additional operation is "GOTO X" because goto operations are considered "special" within the IDE so a unconditional branch operation needed to be it's own operation.
The code for it is:
w <- w + 1
IF w != 0 GOTO X

## Quick work leaves much to be desired ##
There is still a lot to do on this, but it was just a little joke project. Maybe I'll work on it more, maybe I won't. Do what you want with it.

For a list of things to do, look at Info.txt

## Extra ##
Latest class we were given a new operation instruction: The do nothing instruction (a NOP)

* x <- x

*The language L is from "Computability, Complexity, and Languages" Second Edition by Martin D. Davis. Part 1, Chapter 2, Section 1: A Programming Language.*

Note: Mono 3.8 (2.0 profile) and higher compatible (that's the version I have of MoMA downloaded) with only one TODO (which happens to be text highlighting).