**%x% <-0
[a] if x != 0 goto b
goto e
[b] x <- x -1
goto a
**

**%y% <- %x% + %y%
[a] if x != 0 goto b
goto c
[b] y <- y +1
x <- x -1
z <- z +1
if x != 0 goto a
[c] if z != 0 goto d
goto e
[d] z <- z -1
x <- x +1
goto c
**

**%y% <- %x%
[a] if x != 0 goto b
goto c
[b] x <- x -1
y <- y +1
z <- z +1
goto a
[c] if z != 0 goto d
goto e
[d] z <- z -1
x <- x +1
goto c
**

**main
x <- x +1
y <- y +1
x <- x + y
x <-0
**